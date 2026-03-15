namespace AI.FinancialKnowledgeCopilot.Application.Dto;

public sealed record RelevanceCheckResult
{
    /// <summary>True when the query is considered on-topic and may proceed.</summary>
    public bool IsRelevant { get; init; }

    /// <summary>Human-readable reason for rejection, populated only when</summary>
    public string? RejectionReason { get; init; }
}