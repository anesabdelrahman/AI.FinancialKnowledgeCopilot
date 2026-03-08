

using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Services;
using AI.FinancialKnowledgeCopilot.Domain;
using AI.FinancialKnowledgeCopilot.Infrastructure;

namespace AI.FinancialKnowledgeCopilot.Tests;

#region Fakes

internal class FakeEmbeddingService : IEmbeddingService
{
    public float[] EmbeddingToReturn { get; set; } = [0.1f, 0.2f, 0.3f];
    public List<string> ReceivedInputs { get; } = [];

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
    public List<DocumentChunk> StoredChunks { get; } = [];
    public List<DocumentChunk> ChunksToReturn { get; set; } = [];

    public Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        StoredChunks.Add(chunk);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DocumentChunk>> SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<DocumentChunk>>(ChunksToReturn);
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
    private QueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _llmService = new FakeLLMService();
        _sut = new QueryService(_embeddingService, _vectorStore, _llmService);
    }

    [Test]
    public async Task AskAsync_ReturnsAnswerFromLLMService()
    {
        _llmService.AnswerToReturn = "Revenue grew by 12%";
        _vectorStore.ChunksToReturn =
        [
            new DocumentChunk { Id = Guid.NewGuid(), Content = "Some context", Title = "Doc1", Embedding = [0.1f, 0.2f, 0.3f] }
        ];

        var result = await _sut.AskAsync(new QueryRequest { Query = "Revenue?" }, CancellationToken.None);

        Assert.That(result.Answer, Is.EqualTo("Revenue grew by 12%"));
    }

    [Test]
    public async Task AskAsync_PassesQueryToEmbeddingService()
    {
        const string query = "What is the profit margin?";

        await _sut.AskAsync(new QueryRequest { Query = query }, CancellationToken.None);

        Assert.That(_embeddingService.ReceivedInputs, Contains.Item(query));
    }

    [Test]
    public async Task AskAsync_PassesQueryToLLMService()
    {
        const string query = "Earnings per share?";

        await _sut.AskAsync(new QueryRequest { Query = query }, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion, Is.EqualTo(query));
    }

    [Test]
    public async Task AskAsync_SourcesContainChunkTitles()
    {
        _vectorStore.ChunksToReturn =
        [
            new DocumentChunk { Title = "Annual Report 2024", Content = "...", Embedding = [0.1f, 0.2f, 0.3f] },
            new DocumentChunk { Title = "Q3 Earnings",        Content = "...", Embedding = [0.1f, 0.2f, 0.3f] },
        ];

        var result = await _sut.AskAsync(new QueryRequest { Query = "Earnings?" }, CancellationToken.None);

        Assert.That(result.Sources, Is.EquivalentTo(new[] { "Annual Report 2024", "Q3 Earnings" }));
    }

    [Test]
    public async Task AskAsync_WhenNoChunksFound_ReturnsEmptySources()
    {
        _vectorStore.ChunksToReturn = [];

        var result = await _sut.AskAsync(new QueryRequest { Query = "anything" }, CancellationToken.None);

        Assert.That(result.Sources, Is.Empty);
    }

    [Test]
    public async Task AskAsync_PassesChunkContentAsContextToLLM()
    {
        _vectorStore.ChunksToReturn =
        [
            new DocumentChunk { Content = "Profit was $5B", Title = "T1", Embedding = [0.1f, 0.2f, 0.3f] },
            new DocumentChunk { Content = "Revenue was $20B", Title = "T2", Embedding = [0.1f, 0.2f, 0.3f] },
        ];

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
    public async Task IngestAsync_StoresOneChunkPerParagraph()
    {
        const string content = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        await _sut.IngestAsync("Test Doc", content, CancellationToken.None);

        Assert.That(_vectorStore.StoredChunks, Has.Count.EqualTo(3));
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

        Assert.That(_vectorStore.StoredChunks, Has.All.Matches<DocumentChunk>(c => c.Embedding.Length > 0));
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

        Assert.That(_vectorStore.StoredChunks, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task IngestAsync_CallsEmbeddingServiceForEachChunk()
    {
        const string content = "Part A.\n\nPart B.\n\nPart C.";

        await _sut.IngestAsync("Doc", content, CancellationToken.None);

        Assert.That(_embeddingService.ReceivedInputs, Has.Count.EqualTo(3));
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
        var chunk = MakeChunk("Apple earnings 2024", [1f, 0f, 0f]);
        await _sut.StoreAsync(chunk, CancellationToken.None);

        var results = await _sut.SearchAsync([1f, 0f, 0f], 1, CancellationToken.None);

        Assert.That(results.Single().Id, Is.EqualTo(chunk.Id));
    }

    [Test]
    public async Task SearchAsync_ReturnsTopKResults()
    {
        for (int i = 0; i < 10; i++)
            await _sut.StoreAsync(MakeChunk($"Doc {i}", [0.1f, 0.2f, 0.3f]), CancellationToken.None);

        var results = await _sut.SearchAsync([0.1f, 0.2f, 0.3f], 3, CancellationToken.None);

        Assert.That(results.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task SearchAsync_RanksByCosineSimilarity_MostSimilarFirst()
    {
        var highSimilarity = MakeChunk("Exact match", [1f, 0f, 0f]);
        var lowSimilarity = MakeChunk("Poor match", [0f, 1f, 0f]);

        await _sut.StoreAsync(lowSimilarity, CancellationToken.None);
        await _sut.StoreAsync(highSimilarity, CancellationToken.None);

        var results = (await _sut.SearchAsync([1f, 0f, 0f], 2, CancellationToken.None)).ToList();

        Assert.That(results[0].Id, Is.EqualTo(highSimilarity.Id));
    }

    [Test]
    public async Task SearchAsync_WhenStoreIsEmpty_ReturnsEmptyCollection()
    {
        var results = await _sut.SearchAsync([0.1f, 0.2f, 0.3f], 5, CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_WithTopKLargerThanStore_ReturnsAllChunks()
    {
        await _sut.StoreAsync(MakeChunk("A", [1f, 0f, 0f]), CancellationToken.None);
        await _sut.StoreAsync(MakeChunk("B", [0f, 1f, 0f]), CancellationToken.None);

        var results = await _sut.SearchAsync([1f, 0f, 0f], 100, CancellationToken.None);

        Assert.That(results.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var chunk = MakeChunk("Content", [0.6f, 0.8f, 0f]);
        await _sut.StoreAsync(chunk, CancellationToken.None);

        var results = (await _sut.SearchAsync([0.6f, 0.8f, 0f], 1, CancellationToken.None)).ToList();

        Assert.That(results[0].Id, Is.EqualTo(chunk.Id));
    }

    [Test]
    public async Task CosineSimilarity_OrthogonalVectors_ReturnedLast()
    {
        var parallel = MakeChunk("Parallel", [1f, 0f, 0f]);
        var orthogonal = MakeChunk("Orthogonal", [0f, 1f, 0f]);

        await _sut.StoreAsync(parallel, CancellationToken.None);
        await _sut.StoreAsync(orthogonal, CancellationToken.None);

        var results = (await _sut.SearchAsync([1f, 0f, 0f], 2, CancellationToken.None)).ToList();

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