using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface IVectorStore
{
    Task StoreAsync(DocumentChunk chunk, CancellationToken cancellationToken);

    Task<IEnumerable<DocumentChunk>> SearchAsync(float[] embedding, int topK, CancellationToken cancellationToken);
}
