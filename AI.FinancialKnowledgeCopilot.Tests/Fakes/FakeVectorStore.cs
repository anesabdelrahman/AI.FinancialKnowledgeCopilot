using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeVectorStore : IVectorStore
{
    public List<DocumentChunk> StoredChunks { get; } = new();
    public List<ScoredChunk> ChunksToReturn { get; set; } = new();

    public Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        StoredChunks.Add(chunk);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ScoredChunk>> SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<ScoredChunk>>(ChunksToReturn);

    Task<IEnumerable<ScoredChunk>> IVectorStore.SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken)
    {
        ChunksToReturn.Add(new ScoredChunk {Chunk = new DocumentChunk (), Score = 0.01f });
        return Task.FromResult<IEnumerable<ScoredChunk>>(ChunksToReturn);
    }
}
