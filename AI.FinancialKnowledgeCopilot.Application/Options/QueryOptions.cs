namespace AI.FinancialKnowledgeCopilot.Application.Options;

public sealed class QueryOptions
{
    public const string SectionName = "Query";

    /// <summary>
    /// Minimum cosine similarity score [0.0–1.0] a retrieved chunk must meet
    /// for the query to proceed to the LLM. Below this threshold the service
    /// returns a safe "insufficient information" response.
    /// </summary>
    public float ConfidenceThreshold { get; set; } = 0.6f;

    public string LowConfidenceResponse { get; set; } =
        "I was unable to find sufficiently relevant information in the knowledge base " +
        "to answer your question confidently. Please rephrase your query or consult a " +
        "qualified financial adviser.";

    /// <summary>
    /// Maximum permitted character length for an incoming query.
    /// Queries exceeding this are rejected before any processing occurs.
    /// Protects against token-stuffing, prompt injection via large payloads,
    /// and runaway embedding / LLM costs.
    /// Default: 1000 characters (~250 tokens).
    /// </summary>
    public int MaxQueryLength { get; set; } = 1000;

    public string QueryTooLongResponse { get; set; } =
        "Your query is too long. Please shorten your question to under {0} characters and try again.";
}