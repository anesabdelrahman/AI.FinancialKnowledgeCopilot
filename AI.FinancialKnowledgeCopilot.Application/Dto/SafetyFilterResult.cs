namespace AI.FinancialKnowledgeCopilot.Application.Dto;

public sealed record SafetyFilterResult
{
    public string FilteredResponse { get; init; } = string.Empty;

    public bool PiiWasRedacted { get; init; }

    public IReadOnlyList<PiiMatch> RedactedItems { get; init; } = [];

    public bool DisclaimerAppended { get; init; }
}
