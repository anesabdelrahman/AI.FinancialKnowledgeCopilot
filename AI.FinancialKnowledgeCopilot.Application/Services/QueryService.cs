using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public class QueryService : IQueryService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _lLmService;
    const int TOP_K_RESULT = 5;

    public QueryService(IEmbeddingService embeddingService, IVectorStore vectorStore, ILLMService iLLMService)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _lLmService = iLLMService;
    }


    public async Task<QueryResponse> AskAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        var embedding = await _embeddingService.GenerateAsync(request.Query, cancellationToken);
        var chunks = await _vectorStore.SearchAsync(embedding, TOP_K_RESULT, cancellationToken);
        var context = chunks.Select(c => c.Content);
        var answer = await _lLmService.GenerateAnswerAsync(request.Query, context, cancellationToken);

        return new QueryResponse
        {
            Answer = answer,
            Sources = chunks.Select(c => c.Title)
        };
    }
}
