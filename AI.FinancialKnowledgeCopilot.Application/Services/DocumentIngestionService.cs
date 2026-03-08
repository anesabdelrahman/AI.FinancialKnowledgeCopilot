using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Domain;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    public DocumentIngestionService(IEmbeddingService embeddingService, IVectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task IngestAsync(string title, string content, CancellationToken cancellationToken)
    {
        var chunks = Split(content);

        var documentId = Guid.NewGuid();
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddingService.GenerateAsync(chunk, cancellationToken);

            var documentChunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Content = chunk,
                Embedding = embedding,
                Title = title,
            };

            await _vectorStore.StoreAsync(documentChunk, cancellationToken);
        }
    }

    private IEnumerable<string> Split(string content)
    {
        return content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
    }
}
