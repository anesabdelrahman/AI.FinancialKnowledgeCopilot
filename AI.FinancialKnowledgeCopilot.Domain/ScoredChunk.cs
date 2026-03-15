namespace AI.FinancialKnowledgeCopilot.Domain;

public sealed record ScoredChunk
{
    public DocumentChunk Chunk { get; init; } = null!;

    /// <summary>Cosine similarity score in range [0, 1].</summary>
    public float Score { get; init; }
}