namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(string question, CancellationToken cancellationToken);
}
