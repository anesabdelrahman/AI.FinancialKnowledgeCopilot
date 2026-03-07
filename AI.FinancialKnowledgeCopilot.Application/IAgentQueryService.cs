using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Application
{
    public interface IAgentQueryService
    {
        Task<AgentAnswer> AskAsync(string question, CancellationToken cancellationToken);
    }
}