using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Options;
using AI.FinancialKnowledgeCopilot.Application.Services;
using AI.FinancialKnowledgeCopilot.Domain;
using AI.FinancialKnowledgeCopilot.Infrastructure;
using AI.FinancialKnowledgeCopilot.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AI.FinancialKnowledgeCopilot.Tests;

[TestFixture]
public class ScoredSearchTests
{
    private InMemoryVectorStore _sut = null!;

    [SetUp]
    public void SetUp()
    {
        //var env = new FakeDevelopmentEnvironment();
        _sut = new InMemoryVectorStore();
    }

    private static DocumentChunk MakeChunk(string content, float[] embedding) => new()
    {
        Id = Guid.NewGuid(),
        DocumentId = Guid.NewGuid(),
        Content = content,
        Title = "T",
        Embedding = embedding
    };

    [Test]
    public async Task SearchAsync_ReturnsScoreWithEachChunk()
    {
        await _sut.StoreAsync(MakeChunk("Earnings up", [1f, 0f, 0f]), CancellationToken.None);

        var results = (await _sut.SearchAsync([1f, 0f, 0f], 1, CancellationToken.None)).ToList();

        Assert.That(results[0].Score, Is.GreaterThan(0f));
    }

    [Test]
    public async Task SearchAsync_IdenticalVectors_ScoreIsOne()
    {
        await _sut.StoreAsync(MakeChunk("Perfect match", [1f, 0f, 0f]), CancellationToken.None);

        var results = (await _sut.SearchAsync([1f, 0f, 0f], 1, CancellationToken.None)).ToList();

        Assert.That(results[0].Score, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public async Task SearchAsync_OrderedByDescendingScore()
    {
        await _sut.StoreAsync(MakeChunk("Low", [0f, 1f, 0f]), CancellationToken.None);
        await _sut.StoreAsync(MakeChunk("High", [1f, 0f, 0f]), CancellationToken.None);

        var results = (await _sut.SearchAsync([1f, 0f, 0f], 2, CancellationToken.None)).ToList();

        Assert.That(results[0].Score, Is.GreaterThanOrEqualTo(results[1].Score));
    }
}

// ---------------------------------------------------------------------------
// Disclaimer Injection — OutputSafetyFilter
// ---------------------------------------------------------------------------

[TestFixture]
public class DisclaimerInjectionTests
{
    private OutputSafetyFilter BuildSut(bool enabled = true, string[]? keywords = null)
    {
        var options = new DisclaimerOptions
        {
            Enabled = enabled,
            TriggerKeywords = keywords ?? ["invest", "portfolio", "recommend"],
            Text = "\n\n[DISCLAIMER]"
        };
        return new OutputSafetyFilter(
            new RegexPiiDetector(),
            OptionsHelper.For(options),
            NullLogger<OutputSafetyFilter>.Instance);
    }

    [Test]
    [TestCase("Your portfolio returned 8% last year.")]
    [TestCase("I recommend diversifying your investments.")]
    [TestCase("The fund invest strategy is conservative.")]
    public void Apply_ResponseContainsTriggerKeyword_DisclaimerAppended(string response)
    {
        var result = BuildSut().Apply(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.DisclaimerAppended, Is.True);
            Assert.That(result.FilteredResponse, Does.EndWith("[DISCLAIMER]"));
        });
    }

    [Test]
    public void Apply_ResponseHasNoTriggerKeywords_NoDisclaimer()
    {
        var result = BuildSut().Apply("The company was founded in 1998.");

        Assert.Multiple(() =>
        {
            Assert.That(result.DisclaimerAppended, Is.False);
            Assert.That(result.FilteredResponse, Does.Not.Contain("[DISCLAIMER]"));
        });
    }

    [Test]
    public void Apply_DisclaimerDisabled_NeverAppended()
    {
        var result = BuildSut(enabled: false).Apply("I recommend this investment portfolio.");

        Assert.That(result.DisclaimerAppended, Is.False);
    }

    [Test]
    public void Apply_TriggerKeywordCaseInsensitive_DisclaimerAppended()
    {
        var result = BuildSut().Apply("Consider your PORTFOLIO allocation carefully.");

        Assert.That(result.DisclaimerAppended, Is.True);
    }

    [Test]
    public void Apply_ResponseWithPiiAndTrigger_BothApplied()
    {
        var result = BuildSut().Apply("Email user@test.com for investment advice.");

        Assert.Multiple(() =>
        {
            Assert.That(result.PiiWasRedacted, Is.True);
            Assert.That(result.DisclaimerAppended, Is.True);
            Assert.That(result.FilteredResponse, Does.Not.Contain("user@test.com"));
            Assert.That(result.FilteredResponse, Does.EndWith("[DISCLAIMER]"));
        });
    }
}

// ---------------------------------------------------------------------------
// Audit Logging — QueryService emits correct audit events
// ---------------------------------------------------------------------------

[TestFixture]
public class AuditLoggingTests
{
    private FakeEmbeddingService _embeddingService = null!;
    private FakeVectorStore _vectorStore = null!;
    private FakeLLMService _llmService = null!;
    private FakeAuditLogger _auditLogger = null!;
    private FakeQueryRelevanceChecker _queryRelevanceChecker = null!;
    private QueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _llmService = new FakeLLMService();
        _auditLogger = new FakeAuditLogger();
        _queryRelevanceChecker = new FakeQueryRelevanceChecker();

        _sut = BuildQueryService(threshold: 0.5f);
    }

    private QueryService BuildQueryService(float threshold)
    {
        var piiDetector = new RegexPiiDetector();
        var safetyFilter = new OutputSafetyFilter(
            piiDetector,
            OptionsHelper.For(new DisclaimerOptions { Enabled = false }),
            NullLogger<OutputSafetyFilter>.Instance);

        return new QueryService(
            _embeddingService,
            _vectorStore,
            _llmService,
            piiDetector,
            safetyFilter,
            _queryRelevanceChecker,
            _auditLogger,
            OptionsHelper.For(new QueryOptions { ConfidenceThreshold = threshold }),
            OptionsHelper.For(new RelevanceOptions { Enabled = true }), 
            NullLogger<QueryService>.Instance);
    }

    [Test]
    public async Task AskAsync_AlwaysLogsOneAuditEvent()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.9f }
        ];

        await _sut.AskAsync(new QueryRequest { Query = "Revenue?" }, CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AskAsync_AuditEvent_ContainsCorrelationId()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.9f }
        ];

        await _sut.AskAsync(new QueryRequest { Query = "Revenue?" }, CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents[0].CorrelationId, Is.Not.Empty);
    }

    [Test]
    public async Task AskAsync_InputPiiRedacted_CountReflectedInAudit()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.9f }
        ];

        await _sut.AskAsync(
            new QueryRequest { Query = "Check account for user@test.com" },
            CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents[0].InputPiiRedactionCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task AskAsync_LowConfidence_AuditFlagIsSet()
    {
        // No chunks returned → top score = 0 → below any threshold
        _vectorStore.ChunksToReturn = [];

        await _sut.AskAsync(new QueryRequest { Query = "What is the meaning of life?" }, CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents[0].WasLowConfidence, Is.True);
    }

    [Test]
    public async Task AskAsync_LowConfidence_SourcesAreEmpty()
    {
        _vectorStore.ChunksToReturn = [];

        await _sut.AskAsync(new QueryRequest { Query = "Random question" }, CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents[0].Sources, Is.Empty);
    }

    [Test]
    public async Task AskAsync_HighConfidence_AuditRecordsTopScore()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.85f }
        ];

        await _sut.AskAsync(new QueryRequest { Query = "Earnings?" }, CancellationToken.None);

        Assert.That(_auditLogger.LoggedEvents[0].TopConfidenceScore, Is.EqualTo(0.85f).Within(0.001f));
    }
}

// ---------------------------------------------------------------------------
// Confidence Threshold — QueryService short-circuits on low confidence
// ---------------------------------------------------------------------------

[TestFixture]
public class ConfidenceThresholdTests
{
    private FakeEmbeddingService _embeddingService = null!;
    private FakeVectorStore _vectorStore = null!;
    private FakeLLMService _llmService = null!;
    private FakeAuditLogger _auditLogger = null!;
    private FakeQueryRelevanceChecker _queryRelevanceChecker = null!;
    private QueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _llmService = new FakeLLMService();
        _auditLogger = new FakeAuditLogger();
        _queryRelevanceChecker = new FakeQueryRelevanceChecker();

        _sut = BuildQueryService(threshold: 0.5f);
    }

    private QueryService BuildQueryService(float threshold)
    {
        var piiDetector = new RegexPiiDetector();
        var safetyFilter = new OutputSafetyFilter(
            piiDetector,
            OptionsHelper.For(new DisclaimerOptions { Enabled = false }),
            NullLogger<OutputSafetyFilter>.Instance);

        return new QueryService(
            _embeddingService,
            _vectorStore,
            _llmService,
            piiDetector,
            safetyFilter,
            _queryRelevanceChecker,
            _auditLogger,
            OptionsHelper.For(new QueryOptions { ConfidenceThreshold = threshold }),
            OptionsHelper.For(new RelevanceOptions { Enabled = true }),
            NullLogger<QueryService>.Instance);
    }

    [Test]
    public async Task AskAsync_AllChunksBelowThreshold_ReturnsLowConfidenceResponse()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.3f }
        ];

        var result = await BuildQueryService(0.6f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(result.Answer.StartsWith("I was unable to find sufficiently relevant information in the knowledge base"));
    }

    [Test]
    public async Task AskAsync_AllChunksBelowThreshold_LlmIsNotCalled()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.2f }
        ];

        await BuildQueryService(0.6f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion, Is.Null);
    }

    [Test]
    public async Task AskAsync_AllChunksBelowThreshold_SourcesAreEmpty()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.1f }
        ];

        var result = await BuildQueryService(0.6f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(result.Sources, Is.Empty);
    }

    [Test]
    public async Task AskAsync_TopChunkMeetsThreshold_LlmIsCalled()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.8f }
        ];

        await BuildQueryService(0.6f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion, Is.Not.Null);
    }

    [Test]
    public async Task AskAsync_EmptyStore_ReturnsLowConfidenceResponse()
    {
        _vectorStore.ChunksToReturn = [];

        var result = await BuildQueryService(0.6f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(result.Answer.StartsWith("I was unable to find sufficiently relevant information in the"));
    }

    [Test]
    public async Task AskAsync_ThresholdSetToZero_AlwaysProceedsToLlm()
    {
        _vectorStore.ChunksToReturn =
        [
            new ScoredChunk { Chunk = new DocumentChunk { Title = "T", Content = "C", Embedding = [0.1f] }, Score = 0.01f }
        ];

        await BuildQueryService(0f)
            .AskAsync(new QueryRequest { Query = "Query?" }, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion, Is.Not.Null);
    }
}
