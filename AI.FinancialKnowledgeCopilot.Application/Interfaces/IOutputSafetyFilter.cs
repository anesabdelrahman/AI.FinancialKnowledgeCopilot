using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface IOutputSafetyFilter
{
    SafetyFilterResult Apply(string response);
}

public sealed record SafetyFilterResult
{
    public string FilteredResponse { get; init; } = string.Empty;

    public bool PiiWasRedacted { get; init; }

    public IReadOnlyList<PiiMatch> RedactedItems { get; init; } = [];
}
