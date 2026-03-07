using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Infrastructure;

public class OpenAiEmbeddingService : IEmbeddingService
{
    public async Task<float[]> GenerateAsync(string question, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
