using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Options;
using Microsoft.Extensions.Options;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

/// <summary>
/// Keyword-based financial domain relevance checker.
///
/// Checks the query (case-insensitively) against a configurable list of financial
/// keywords. The query must contain at least <see cref="RelevanceOptions.MinKeywordMatches"/>
/// matches to be considered on-topic.
///
/// This approach has zero latency and no external dependencies. For higher accuracy
/// on ambiguous queries, replace or supplement with an LLM-based classifier.
/// </summary>
public sealed class KeywordQueryRelevanceChecker(
    IOptions<RelevanceOptions> options) : IQueryRelevanceChecker
{
    private readonly RelevanceOptions _options = options.Value;

    public RelevanceCheckResult Check(string query)
    {
        // When disabled, all queries are considered relevant
        if (!_options.Enabled)
            return new RelevanceCheckResult { IsRelevant = true };

        if (string.IsNullOrWhiteSpace(query))
            return new RelevanceCheckResult
            {
                IsRelevant = false,
                RejectionReason = "Query was empty."
            };

        var matchCount = _options.FinancialKeywords
            .Count(keyword => query.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (matchCount >= _options.MinKeywordMatches)
            return new RelevanceCheckResult { IsRelevant = true };

        return new RelevanceCheckResult
        {
            IsRelevant = false,
            RejectionReason =
                $"Query did not match any financial domain keywords " +
                $"(required: {_options.MinKeywordMatches}, found: {matchCount})."
        };
    }
}