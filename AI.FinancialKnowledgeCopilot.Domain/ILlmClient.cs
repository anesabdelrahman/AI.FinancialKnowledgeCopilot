
namespace AI.FinancialKnowledgeCopilot.Domain;

public interface ILlmClient
{
    Task<AgentAnswer> GenerateAsync(object prompt, CancellationToken cancellationToken);
}
