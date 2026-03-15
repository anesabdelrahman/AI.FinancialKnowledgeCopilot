using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeEmbeddingService : IEmbeddingService
{
    public float[] EmbeddingToReturn { get; set; } = new float[] { 0.1f, 0.2f, 0.3f };
    public List<string> ReceivedInputs { get; } = new();

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        ReceivedInputs.Add(text);
        return Task.FromResult(EmbeddingToReturn);
    }
}
