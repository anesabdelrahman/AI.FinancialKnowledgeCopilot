using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public class QueryService : IQueryService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly IPiiDetector _piiDetector;
    private readonly IOutputSafetyFilter _outputSafetyFilter;
    private readonly ILogger<QueryService> _logger;
    const int TOP_K_RESULT = 5;

    public QueryService(IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService,
        IPiiDetector piiDetector,
        IOutputSafetyFilter outputSafetyFilter,
        ILogger<QueryService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _piiDetector = piiDetector;
        _outputSafetyFilter = outputSafetyFilter;
        _logger = logger;
    }


    public async Task<QueryResponse> AskAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        // --- INPUT GUARDRAIL: scrub PII from the query before it leaves this service ---
        var inputScrub = _piiDetector.Scrub(request.Query);

        if (inputScrub.HasPii)
        {
            _logger.LogWarning("PII detected and removed from incoming query. Types: {Types}",
                string.Join(", ", inputScrub.Findings.Select(f => f.Type)));
        }

        var safeQuery = inputScrub.ScrubbedText;

        // --- RAG pipeline ---
        var embedding = await _embeddingService.GenerateAsync(safeQuery, cancellationToken);
        var chunks = await _vectorStore.SearchAsync(embedding, TOP_K_RESULT, cancellationToken);
        var chunkList = chunks.ToList();
        var context = chunks.Select(c => c.Content);
        var rawAnswer = await _llmService.GenerateAnswerAsync(safeQuery, context, cancellationToken);

        // --- OUTPUT GUARDRAIL: scrub PII from the LLM response before returning ---
        var safeResult = _outputSafetyFilter.Apply(rawAnswer);

        return new QueryResponse
        {
            Answer = safeResult.FilteredResponse,
            Sources = chunkList.Select(c => c.Title)
        };
    }
}
