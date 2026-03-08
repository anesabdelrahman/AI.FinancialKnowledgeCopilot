using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Services;
using AI.FinancialKnowledgeCopilot.Domain;
using AI.FinancialKnowledgeCopilot.Infrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI.FinancialKnowledgeCopilot.Tests;

#region Fakes

internal class FakeEmbeddingService : IEmbeddingService
{
    public float[] EmbeddingToReturn { get; set; } = new float[] { 0.1f, 0.2f, 0.3f };
    public List<string> ReceivedInputs { get; } = new();

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        ReceivedInputs.Add(text);
        return Task.FromResult(EmbeddingToReturn);
    }
}

internal class FakeLLMService : ILLMService
{
    public string AnswerToReturn { get; set; } = "Fake LLM answer";
    public string? ReceivedQuestion { get; private set; }
    public IEnumerable<string>? ReceivedContext { get; private set; }

    public Task<string> GenerateAnswerAsync(string question, IEnumerable<string> context, CancellationToken cancellationToken)
    {
        ReceivedQuestion = question;
        ReceivedContext = context;
        return Task.FromResult(AnswerToReturn);
    }
}

internal class FakeVectorStore : IVectorStore
{
    public List<DocumentChunk> StoredChunks { get; } = new();
    public List<DocumentChunk> ChunksToReturn { get; set; } = new();

    public Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        StoredChunks.Add(chunk);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DocumentChunk>> SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<DocumentChunk>>(ChunksToReturn);
}

internal class FakePiiDetector : IPiiDetector
{
    public bool ContainsPii(string text)
    {
        return text.Length > 0;
    }

    public PiiDetectionResult Scrub(string text)
    {
        return new PiiDetectionResult
        {
            Findings = new List<PiiMatch>(),
            ScrubbedText = $"{text} - scrubbed"
        };
    }
}

internal class FakeOutputSafetyFilter : IOutputSafetyFilter
{
    public SafetyFilterResult Apply(string response)
    {
        return new SafetyFilterResult
        {
            FilteredResponse = $"{response}  - scrubbedd",
            PiiWasRedacted = true,
            RedactedItems = new List<PiiMatch>()
        };
    }
}

#endregion

// ---------------------------------------------------------------------------
// QueryService
// ---------------------------------------------------------------------------

[TestFixture]
public class QueryServiceTests
{
    private FakeEmbeddingService _embeddingService = null!;
    private FakeVectorStore _vectorStore = null!;
    private FakeLLMService _llmService = null!;
    private FakePiiDetector _piiDetector = null!;
    private FakeOutputSafetyFilter _safetyFilter = null!;

    private Logger<QueryService> _logger;

    private QueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _llmService = new FakeLLMService();
        _piiDetector = new FakePiiDetector();
        _safetyFilter = new FakeOutputSafetyFilter();

        _sut = new QueryService(_embeddingService, _vectorStore, _llmService, _piiDetector, _safetyFilter, _logger);
    }

    [Test]
    public async Task AskAsync_ReturnsAnswerFromLLMService()
    {
        _llmService.AnswerToReturn = "Revenue grew by 12%";
        _vectorStore.ChunksToReturn = new List<DocumentChunk>
        {
            new DocumentChunk { Id = Guid.NewGuid(), Content = "Some context", Title = "Doc1", Embedding = new float[] { 0.1f, 0.2f, 0.3f } }
        };

        var result = await _sut.AskAsync(new QueryRequest { Query = "Revenue?" }, CancellationToken.None);

        Assert.That(result.Answer.Contains("Revenue grew by 12%"));
    }

    [Test]
    public async Task AskAsync_PassesQueryToEmbeddingService()
    {
        const string query = "What is the profit margin?";

        await _sut.AskAsync(new QueryRequest { Query = query }, CancellationToken.None);

        Assert.That(_embeddingService.ReceivedInputs.Any(s => s.Contains(query)));
    }

    [Test]
    public async Task AskAsync_PassesQueryToLLMService()
    {
        const string query = "Earnings per share?";

        await _sut.AskAsync(new QueryRequest { Query = query }, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion.Contains(query));
    }

    [Test]
    public async Task AskAsync_SourcesContainChunkTitles()
    {
        _vectorStore.ChunksToReturn = new List<DocumentChunk>
        {
            new DocumentChunk { Title = "Annual Report 2024", Content = "...", Embedding = new float[] { 0.1f, 0.2f, 0.3f } },
            new DocumentChunk { Title = "Q3 Earnings",        Content = "...", Embedding = new float[] { 0.1f, 0.2f, 0.3f } },
        };

        var result = await _sut.AskAsync(new QueryRequest { Query = "Earnings?" }, CancellationToken.None);

        Assert.That(result.Sources, Is.EquivalentTo(new[] { "Annual Report 2024", "Q3 Earnings" }));
    }

    [Test]
    public async Task AskAsync_WhenNoChunksFound_ReturnsEmptySources()
    {
        _vectorStore.ChunksToReturn = new List<DocumentChunk>();

        var result = await _sut.AskAsync(new QueryRequest { Query = "anything" }, CancellationToken.None);

        Assert.That(result.Sources, Is.Empty);
    }

    [Test]
    public async Task AskAsync_PassesChunkContentAsContextToLLM()
    {
        _vectorStore.ChunksToReturn = new List<DocumentChunk>
        {
            new DocumentChunk { Content = "Profit was $5B", Title = "T1", Embedding = new float[] { 0.1f, 0.2f, 0.3f } },
            new DocumentChunk { Content = "Revenue was $20B", Title = "T2", Embedding = new float[] { 0.1f, 0.2f, 0.3f } },
        };

        await _sut.AskAsync(new QueryRequest { Query = "Financials?" }, CancellationToken.None);

        Assert.That(_llmService.ReceivedContext, Is.EquivalentTo(new[] { "Profit was $5B", "Revenue was $20B" }));
    }
}

// ---------------------------------------------------------------------------
// DocumentIngestionService
// ---------------------------------------------------------------------------

[TestFixture]
public class DocumentIngestionServiceTests
{
    private FakeEmbeddingService _embeddingService = null!;
    private FakeVectorStore _vectorStore = null!;
    private DocumentIngestionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _sut = new DocumentIngestionService(_embeddingService, _vectorStore);
    }

    [Test]
    public async Task IngestAsync_ShortContent_ProducesSingleChunk()
    {
        const string content = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        await _sut.IngestAsync("Test Doc", content, CancellationToken.None);

        // Default Split behavior batches short paragraphs into chunks; with small text expect a single chunk
        Assert.That(_vectorStore.StoredChunks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task IngestAsync_LongContent_SplitsAndPreservesOverlap()
    {
        // Create long paragraphs to force splitting. Each paragraph length chosen so combined content exceeds default maxChunkLength (500).
        var longPara = new string('A', 300);
        var content = $"{longPara}\n\n{longPara}\n\n{longPara}";

        await _sut.IngestAsync("LongDoc", content, CancellationToken.None);

        Assert.That(_vectorStore.StoredChunks.Count, Is.GreaterThan(1), "Expected multiple chunks for long content.");

        // Validate overlap between consecutive chunks (default overlap is 50 characters).
        var overlap = 50;
        var chunks = _vectorStore.StoredChunks.Select(c => c.Content).ToList();

        for (int i = 0; i < chunks.Count - 1; i++)
        {
            var tail = chunks[i].Length >= overlap ? chunks[i].AsSpan(chunks[i].Length - overlap, overlap).ToString() : string.Empty;
            var head = chunks[i + 1].Length >= overlap ? chunks[i + 1].AsSpan(0, overlap).ToString() : string.Empty;
            Assert.That(tail, Is.Not.Empty.And.EqualTo(head), $"Chunk {i} tail and chunk {i+1} head must match for overlap.");
        }
    }

    [Test]
    public async Task IngestAsync_AllChunksShareSameDocumentId()
    {
        const string content = "First chunk.\n\nSecond chunk.";

        await _sut.IngestAsync("Doc", content, CancellationToken.None);

        var documentIds = _vectorStore.StoredChunks.Select(c => c.DocumentId).Distinct();
        Assert.That(documentIds, Has.Exactly(1).Items);
    }

    [Test]
    public async Task IngestAsync_ChunkTitleMatchesDocumentTitle()
    {
        const string title = "Quarterly Report Q1";

        await _sut.IngestAsync(title, "Only one chunk.", CancellationToken.None);

        Assert.That(_vectorStore.StoredChunks.Single().Title, Is.EqualTo(title));
    }

    [Test]
    public async Task IngestAsync_EachChunkHasEmbedding()
    {
        await _sut.IngestAsync("Doc", "Chunk one.\n\nChunk two.", CancellationToken.None);

        Assert.That(_vectorStore.StoredChunks, Has.All.Matches<DocumentChunk>(c => c.Embedding is { Length: > 0 }));
    }

    [Test]
    public async Task IngestAsync_EachChunkHasUniqueId()
    {
        await _sut.IngestAsync("Doc", "A.\n\nB.\n\nC.", CancellationToken.None);

        var ids = _vectorStore.StoredChunks.Select(c => c.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
    }

    [Test]
    public async Task IngestAsync_IgnoresEmptyParagraphs()
    {
        // Double blank lines produce empty entries — should be removed
        const string content = "Para one.\n\n\n\nPara two.";

        await _sut.IngestAsync("Doc", content, CancellationToken.None);

        Assert.That(_vectorStore.StoredChunks.Count, Is.GreaterThan(0));
        // Ensure no stored chunk content is purely whitespace
        Assert.That(_vectorStore.StoredChunks.All(c => !string.IsNullOrWhiteSpace(c.Content)));
    }

    [Test]
    public async Task IngestAsync_CallsEmbeddingServiceForEachChunk()
    {
        const string content = "Part A.\n\nPart B.\n\nPart C.";

        await _sut.IngestAsync("Doc", content, CancellationToken.None);

        Assert.That(_embeddingService.ReceivedInputs.Count, Is.EqualTo(_vectorStore.StoredChunks.Count));
    }
}

// ---------------------------------------------------------------------------
// InMemoryVectorStore
// ---------------------------------------------------------------------------

[TestFixture]
public class InMemoryVectorStoreTests
{
    private InMemoryVectorStore _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new InMemoryVectorStore();

    private static DocumentChunk MakeChunk(string content, float[] embedding) =>
        new() { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Content = content, Title = "T", Embedding = embedding };

    [Test]
    public async Task StoreAsync_AndSearchAsync_ReturnsStoredChunk()
    {
        var chunk = MakeChunk("Apple earnings 2024", new float[] { 1f, 0f, 0f });
        await _sut.StoreAsync(chunk, CancellationToken.None);

        var results = await _sut.SearchAsync(new float[] { 1f, 0f, 0f }, 1, CancellationToken.None);

        Assert.That(results.Single().Id, Is.EqualTo(chunk.Id));
    }

    [Test]
    public async Task SearchAsync_ReturnsTopKResults()
    {
        for (int i = 0; i < 10; i++)
            await _sut.StoreAsync(MakeChunk($"Doc {i}", new float[] { 0.1f, 0.2f, 0.3f }), CancellationToken.None);

        var results = await _sut.SearchAsync(new float[] { 0.1f, 0.2f, 0.3f }, 3, CancellationToken.None);

        Assert.That(results.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task SearchAsync_RanksByCosineSimilarity_MostSimilarFirst()
    {
        var highSimilarity = MakeChunk("Exact match", new float[] { 1f, 0f, 0f });
        var lowSimilarity = MakeChunk("Poor match", new float[] { 0f, 1f, 0f });

        await _sut.StoreAsync(lowSimilarity, CancellationToken.None);
        await _sut.StoreAsync(highSimilarity, CancellationToken.None);

        var results = (await _sut.SearchAsync(new float[] { 1f, 0f, 0f }, 2, CancellationToken.None)).ToList();

        Assert.That(results[0].Id, Is.EqualTo(highSimilarity.Id));
    }

    [Test]
    public async Task SearchAsync_WhenStoreIsEmpty_ReturnsEmptyCollection()
    {
        var results = await _sut.SearchAsync(new float[] { 0.1f, 0.2f, 0.3f }, 5, CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_WithTopKLargerThanStore_ReturnsAllChunks()
    {
        await _sut.StoreAsync(MakeChunk("A", new float[] { 1f, 0f, 0f }), CancellationToken.None);
        await _sut.StoreAsync(MakeChunk("B", new float[] { 0f, 1f, 0f }), CancellationToken.None);

        var results = await _sut.SearchAsync(new float[] { 1f, 0f, 0f }, 100, CancellationToken.None);

        Assert.That(results.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var chunk = MakeChunk("Content", new float[] { 0.6f, 0.8f, 0f });
        await _sut.StoreAsync(chunk, CancellationToken.None);

        var results = (await _sut.SearchAsync(new float[] { 0.6f, 0.8f, 0f }, 1, CancellationToken.None)).ToList();

        Assert.That(results[0].Id, Is.EqualTo(chunk.Id));
    }

    [Test]
    public async Task CosineSimilarity_OrthogonalVectors_ReturnedLast()
    {
        var parallel = MakeChunk("Parallel", new float[] { 1f, 0f, 0f });
        var orthogonal = MakeChunk("Orthogonal", new float[] { 0f, 1f, 0f });

        await _sut.StoreAsync(parallel, CancellationToken.None);
        await _sut.StoreAsync(orthogonal, CancellationToken.None);

        var results = (await _sut.SearchAsync(new float[] { 1f, 0f, 0f }, 2, CancellationToken.None)).ToList();

        Assert.That(results.Last().Id, Is.EqualTo(orthogonal.Id));
    }
}

// ---------------------------------------------------------------------------
// Domain model integrity
// ---------------------------------------------------------------------------

[TestFixture]
public class DomainModelTests
{
    [Test]
    public void FinancialDocument_DefaultValues_AreValid()
    {
        var doc = new FinancialDocument();

        Assert.Multiple(() =>
        {
            Assert.That(doc.Title, Is.EqualTo(string.Empty));
            Assert.That(doc.Content, Is.EqualTo(string.Empty));
            Assert.That(doc.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
        });
    }

    [Test]
    public void DocumentChunk_DefaultEmbedding_IsEmpty()
    {
        var chunk = new DocumentChunk();

        Assert.That(chunk.Embedding, Is.Empty);
    }

    [Test]
    public void QueryRequest_DefaultQuery_IsEmpty()
    {
        var req = new QueryRequest();

        Assert.That(req.Query, Is.EqualTo(string.Empty));
    }

    [Test]
    public void QueryResponse_DefaultSources_IsEmpty()
    {
        var resp = new QueryResponse();

        Assert.That(resp.Sources, Is.Empty);
    }
}