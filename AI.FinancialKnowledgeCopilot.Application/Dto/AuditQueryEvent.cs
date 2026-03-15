namespace AI.FinancialKnowledgeCopilot.Application.Dto;

/// <summary>
/// Immutable record of a single query/response cycle.
/// </summary>
public sealed record AuditQueryEvent
{
    /// <summary>Unique identifier for this request; correlates logs across services.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>UTC timestamp when the query was received.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The user's query after PII scrubbing (may be truncated if too long).</summary>
    public required string ScrubbedQuery { get; init; }

    /// <summary>The final response after all safety filters have been applied.</summary>
    public required string ScrubbedResponse { get; init; }

    /// <summary>Titles of source documents used to ground the answer.</summary>
    public IReadOnlyList<string> Sources { get; init; } = [];

    /// <summary>Highest cosine similarity score among retrieved chunks. 0 if not reached.</summary>
    public float TopConfidenceScore { get; init; }

    // ── Rejection flags — only one can be true per event ──────────────────

    /// <summary>Query exceeded the configured maximum character length.</summary>
    public bool WasRejectedForLength { get; init; }

    /// <summary>Query was classified as off-topic for the financial domain.</summary>
    public bool WasRejectedForRelevance { get; init; }

    /// <summary>No retrieved chunk met the confidence threshold.</summary>
    public bool WasLowConfidence { get; init; }

    // ── PII & safety counters ──────────────────────────────────────────────

    /// <summary>Number of PII items redacted from the input query.</summary>
    public int InputPiiRedactionCount { get; init; }

    /// <summary>Number of PII items redacted from the LLM output.</summary>
    public int OutputPiiRedactionCount { get; init; }

    /// <summary>Whether a financial disclaimer was appended to the response.</summary>
    public bool DisclaimerAppended { get; init; }
}