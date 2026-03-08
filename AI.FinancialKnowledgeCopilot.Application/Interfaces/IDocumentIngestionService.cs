using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces
{
    public interface IDocumentIngestionService
    {
        Task IngestAsync(string title, string content, CancellationToken cancellationToken);
    }
}