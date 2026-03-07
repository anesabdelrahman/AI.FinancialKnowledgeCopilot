
namespace AI.FinancialKnowledgeCopilot.Domain;

public interface IVectorStore
{
    Task<string> SearchAsync(float[] vector, int v, CancellationToken cancellationToken);
}
