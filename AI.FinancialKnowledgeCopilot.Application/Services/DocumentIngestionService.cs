using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Domain;
using System.Text;

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

    private IEnumerable<string> Split(string content, int maxChunkLength = 500, int overlap = 50)
    {
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (current.Length + paragraph.Length > maxChunkLength && current.Length > 0)
            {
                yield return current.ToString();

                var tail = current.ToString();
                current.Clear();

                if (tail.Length > overlap)
                    current.Append(tail[^overlap..]);
            }
            current.AppendLine(paragraph);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }
}
