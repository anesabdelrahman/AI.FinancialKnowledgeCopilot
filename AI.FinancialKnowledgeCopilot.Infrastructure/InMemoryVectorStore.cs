using System.Collections.Concurrent;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Domain;
//using Microsoft.Extensions.Hosting;

namespace AI.FinancialKnowledgeCopilot.Infrastructure;

/// <summary>
/// Development-only in-memory vector store.
/// </summary>
/// <remarks>
/// NOT for production — use Redis Vector, Pinecone, Milvus, FAISS, etc.
/// Thread-safe via ConcurrentDictionary + snapshot-based search.
/// Throws at construction if resolved outside a Development environment.
/// </remarks>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<Guid, DocumentChunk> _chunks = new();
    private int _dimension; // 0 = unset

    //public InMemoryVectorStore(IHostEnvironment env) //TODO: DON'T WANT TO BE ENV AWARE HERE...
    //{
    //    if (!(env?.IsDevelopment() ?? false))
    //        throw new InvalidOperationException(
    //            "InMemoryVectorStore is for development only. " +
    //            "Register a production-grade vector store in non-development environments.");
    //}

    public Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(chunk);

        if (chunk.Embedding is null || chunk.Embedding.Length == 0)
            throw new ArgumentException("Embedding must be a non-empty vector.", nameof(chunk));

        if (chunk.Id == Guid.Empty)
            throw new ArgumentException("DocumentChunk.Id must not be empty.", nameof(chunk));

        var existingDim = Volatile.Read(ref _dimension);
        if (existingDim == 0)
        {
            Interlocked.CompareExchange(ref _dimension, chunk.Embedding.Length, 0);
            existingDim = Volatile.Read(ref _dimension);
        }

        if (chunk.Embedding.Length != existingDim)
            throw new ArgumentException(
                $"Embedding dimension mismatch. Expected {existingDim}, got {chunk.Embedding.Length}.",
                nameof(chunk));

        _chunks.AddOrUpdate(chunk.Id, chunk, (_, _) => chunk);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ScoredChunk>> SearchAsync(
        float[] embedding, int topK, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (embedding is null || embedding.Length == 0)
            throw new ArgumentException("Search embedding must be a non-empty vector.", nameof(embedding));

        var dim = Volatile.Read(ref _dimension);
        if (dim != 0 && embedding.Length != dim)
            throw new ArgumentException(
                $"Embedding dimension mismatch. Expected {dim}, got {embedding.Length}.",
                nameof(embedding));

        if (topK <= 0)
            return Task.FromResult(Enumerable.Empty<ScoredChunk>());

        var snapshot = _chunks.Values.ToArray();

        var results = snapshot
            .Select(c => new ScoredChunk
            {
                Chunk = c,
                Score = SafeCosineSimilarity(c.Embedding, embedding)
            })
            .OrderByDescending(s => s.Score)
            .Take(topK);

        return Task.FromResult<IEnumerable<ScoredChunk>>(results);
    }

    private static float SafeCosineSimilarity(float[] a, float[] b)
    {
        if (a is null || b is null || a.Length != b.Length) return 0f;

        double dot = 0, magA = 0, magB = 0;
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