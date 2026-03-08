using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces
{
    public interface IQueryService
    {
        Task<QueryResponse> AskAsync(QueryRequest request, CancellationToken cancellationToken);
    }
}