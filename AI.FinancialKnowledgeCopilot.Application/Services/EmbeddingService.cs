using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Application.Services;
public class EmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateAsync(string question, CancellationToken cancellationToken)
    {
        //TODO: Need to integrate with an AI embedding model 
        var embeddings = new float[] { 0.02f, 0.36f, .91f };
        return Task.FromResult(embeddings);
    }
}