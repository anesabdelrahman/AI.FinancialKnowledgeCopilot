using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public class QueryService : IQueryService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly IPiiDetector _piiDetector;
    private readonly IOutputSafetyFilter _outputSafetyFilter;
    private readonly IQueryRelevanceChecker _relevanceChecker;
    private readonly IAuditLogger _auditLogger;
    private readonly QueryOptions _queryOptions;
    private readonly RelevanceOptions _relevanceOptions;
    private readonly ILogger<QueryService> _logger;

    private const int TopKResult = 5;

    public QueryService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService,
        IPiiDetector piiDetector,
        IOutputSafetyFilter outputSafetyFilter,
        IQueryRelevanceChecker relevanceChecker,
        IAuditLogger auditLogger,
        IOptions<QueryOptions> queryOptions,
        IOptions<RelevanceOptions> relevanceOptions,
        ILogger<QueryService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _piiDetector = piiDetector;
        _outputSafetyFilter = outputSafetyFilter;
        _relevanceChecker = relevanceChecker;
        _auditLogger = auditLogger;
        _queryOptions = queryOptions.Value;
        _relevanceOptions = relevanceOptions.Value;
        _logger = logger;
    }

    public async Task<QueryResponse> AskAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        // ── GUARDRAIL 1: Input length check ───────────────────────────────────
        if (request.Query.Length > _queryOptions.MaxQueryLength)
        {
            _logger.LogWarning(
                "Query rejected: too long. CorrelationId={CorrelationId} Length={Length} Max={Max}",
                correlationId, request.Query.Length, _queryOptions.MaxQueryLength);

            var tooLongResponse = string.Format(
                _queryOptions.QueryTooLongResponse, _queryOptions.MaxQueryLength);

            _auditLogger.LogQuery(new AuditQueryEvent
            {
                CorrelationId = correlationId,
                ScrubbedQuery = $"[QUERY TRUNCATED — {request.Query.Length} chars]",
                ScrubbedResponse = tooLongResponse,
                WasRejectedForLength = true
            });

            return new QueryResponse { Answer = tooLongResponse, Sources = [] };
        }

        // ── GUARDRAIL 2: Relevance check ──────────────────────────────────────
        var relevanceResult = _relevanceChecker.Check(request.Query);
        if (!relevanceResult.IsRelevant)
        {
            _logger.LogWarning(
                "Query rejected: off-topic. CorrelationId={CorrelationId} Reason={Reason}",
                correlationId, relevanceResult.RejectionReason);

            _auditLogger.LogQuery(new AuditQueryEvent
            {
                CorrelationId = correlationId,
                ScrubbedQuery = request.Query,
                ScrubbedResponse = _relevanceOptions.OffTopicResponse,
                WasRejectedForRelevance = true
            });

            return new QueryResponse { Answer = _relevanceOptions.OffTopicResponse, Sources = [] };
        }

        // ── GUARDRAIL 3: PII scrubbing (input) ───────────────────────────────
        var inputScrub = _piiDetector.Scrub(request.Query);
        if (inputScrub.HasPii)
        {
            _logger.LogWarning(
                "PII removed from incoming query. CorrelationId={CorrelationId} Types={Types}",
                correlationId,
                string.Join(", ", inputScrub.Findings.Select(f => f.Type)));
        }

        var safeQuery = inputScrub.ScrubbedText;

        // ── RETRIEVAL ─────────────────────────────────────────────────────────
        var embedding = await _embeddingService.GenerateAsync(safeQuery, cancellationToken);
        var scoredChunks = (await _vectorStore.SearchAsync(embedding, TopKResult, cancellationToken)).ToList();

        // ── GUARDRAIL 4: Confidence threshold ────────────────────────────────
        var topScore = scoredChunks.Count > 0 ? scoredChunks[0].Score : 0f;
        if (topScore < _queryOptions.ConfidenceThreshold)
        {
            _logger.LogWarning(
                "Low confidence retrieval. CorrelationId={CorrelationId} TopScore={TopScore} Threshold={Threshold}",
                correlationId, topScore, _queryOptions.ConfidenceThreshold);

            _auditLogger.LogQuery(new AuditQueryEvent
            {
                CorrelationId = correlationId,
                ScrubbedQuery = safeQuery,
                ScrubbedResponse = _queryOptions.LowConfidenceResponse,
                TopConfidenceScore = topScore,
                WasLowConfidence = true,
                InputPiiRedactionCount = inputScrub.Findings.Count
            });

            return new QueryResponse { Answer = _queryOptions.LowConfidenceResponse, Sources = [] };
        }

        // ── LLM CALL ──────────────────────────────────────────────────────────
        var context = scoredChunks.Select(s => s.Chunk.Content);
        var rawAnswer = await _llmService.GenerateAnswerAsync(safeQuery, context, cancellationToken);

        // ── GUARDRAIL 5: Output safety (PII scrub + disclaimer) ───────────────
        var safetyResult = _outputSafetyFilter.Apply(rawAnswer);
        var sources = scoredChunks.Select(s => s.Chunk.Title).ToList();

        // ── AUDIT LOG ─────────────────────────────────────────────────────────
        _auditLogger.LogQuery(new AuditQueryEvent
        {
            CorrelationId = correlationId,
            ScrubbedQuery = safeQuery,
            ScrubbedResponse = safetyResult.FilteredResponse,
            Sources = sources,
            TopConfidenceScore = topScore,
            InputPiiRedactionCount = inputScrub.Findings.Count,
            OutputPiiRedactionCount = safetyResult.RedactedItems.Count,
            DisclaimerAppended = safetyResult.DisclaimerAppended
        });

        return new QueryResponse
        {
            Answer = safetyResult.FilteredResponse,
            Sources = sources
        };
    }
}