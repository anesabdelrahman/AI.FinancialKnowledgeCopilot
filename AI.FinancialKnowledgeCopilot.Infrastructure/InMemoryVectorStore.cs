using System.Collections.Concurrent;
using AI.FinancialKnowledgeCopilot.Domain;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Infrastructure;

/// <summary>
/// Development-only in-memory vector store.
/// </summary>
/// <remarks>
/// - NOT for production: use a production vector DB / ANN (Redis Vector, Pinecone, Milvus, FAISS, etc.).
/// - Thread-safe snapshot-based search and basic sanity checks are applied.
/// - The constructor will throw if resolved in a non-Development environment to avoid accidental production use.
/// </remarks>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<Guid, DocumentChunk> _chunks = new();
    private int _dimension; // 0 = unknown

    public Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (chunk is null)
        {
            throw new ArgumentNullException(nameof(chunk));
        }

        if (chunk.Embedding is null || chunk.Embedding.Length == 0)
        {
            throw new ArgumentException("DocumentChunk.Embedding must be a non-empty vector.", nameof(chunk));
        }

        // Establish dimension on first write, and enforce for subsequent writes
        var existingDim = Volatile.Read(ref _dimension);
        if (existingDim == 0)
        {
            Interlocked.CompareExchange(ref _dimension, chunk.Embedding.Length, 0);
            existingDim = Volatile.Read(ref _dimension);
        }

        if (chunk.Embedding.Length != existingDim)
        {
            throw new ArgumentException($"Embedding dimension mismatch. Expected {existingDim}, got {chunk.Embedding.Length}.", nameof(chunk));
        }

        // Upsert chunk
        if (chunk.Id == Guid.Empty)
            throw new ArgumentException("DocumentChunk Id must not be empty");

        _chunks.AddOrUpdate(chunk.Id, chunk, (_, __) => chunk);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<DocumentChunk>> SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (embedding is null || embedding.Length == 0)
        {
            throw new ArgumentException("Search embedding must be a non-empty vector.", nameof(embedding));
        }

        var dim = Volatile.Read(ref _dimension);
        if (dim != 0 && embedding.Length != dim)
        {
            throw new ArgumentException($"Embedding dimension mismatch. Expected {dim}, got {embedding.Length}.", nameof(embedding));
        }

        if (topK <= 0)
        {
            return Task.FromResult(Enumerable.Empty<DocumentChunk>());
        }

        // Take a thread-safe snapshot of values for searching
        var snapshot = _chunks.Values.ToArray();

        var scored = snapshot
            .Select(c => (chunk: c, score: SafeCosineSimilarity(c.Embedding, embedding)))
            .OrderByDescending(t => t.score)
            .Take(topK)
            .Select(t => t.chunk);

        return Task.FromResult<IEnumerable<DocumentChunk>>(scored);
    }

    private static float SafeCosineSimilarity(float[] a, float[] b)
    {
        if (a is null || b is null) return 0f;
        if (a.Length != b.Length) return 0f;

        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA <= double.Epsilon || magB <= double.Epsilon) return 0f;

        return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB)));
    }
}
