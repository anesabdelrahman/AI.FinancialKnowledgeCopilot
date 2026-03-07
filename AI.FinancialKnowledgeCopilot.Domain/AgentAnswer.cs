namespace AI.FinancialKnowledgeCopilot.Domain;

public sealed record AgentAnswer
{
    public string Response { get; init; }
    public IReadOnlyList<string> Sources { get; init; }
    public bool IsValidated { get; init; }
}
