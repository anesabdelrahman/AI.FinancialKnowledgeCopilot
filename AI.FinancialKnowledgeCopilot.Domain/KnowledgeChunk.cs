namespace AI.FinancialKnowledgeCopilot.Domain;

public sealed record KnowledgeChunk
{
    public string Id { get; init; }
    public string Content { get; init; }
    public string Source { get; init; }
}
