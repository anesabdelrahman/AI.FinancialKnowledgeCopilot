
namespace AI.FinancialKnowledgeCopilot.Domain;

public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(string question, CancellationToken cancellationToken);
}
