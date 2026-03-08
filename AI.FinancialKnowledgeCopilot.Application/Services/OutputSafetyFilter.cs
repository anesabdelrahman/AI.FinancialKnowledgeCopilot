using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

/// <summary>
/// Applies output-side safety guardrails to raw LLM responses.
/// Currently: PII scrubbing. Extend here for toxicity filtering,
/// hallucination checks, disclaimer injection, etc.
/// </summary>
public sealed class OutputSafetyFilter(
    IPiiDetector piiDetector,
    ILogger<OutputSafetyFilter> logger) : IOutputSafetyFilter
{
    public SafetyFilterResult Apply(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new SafetyFilterResult { FilteredResponse = response };

        var piiResult = piiDetector.Scrub(response);

        if (piiResult.HasPii)
        {
            // Log that redaction happened without logging the actual PII values
            logger.LogWarning(
                "PII detected and redacted from LLM response. " +
                "Types found: {PiiTypes}",
                string.Join(", ", piiResult.Findings.Select(f => f.Type)));
        }

        return new SafetyFilterResult
        {
            FilteredResponse = piiResult.ScrubbedText,
            PiiWasRedacted = piiResult.HasPii,
            RedactedItems = piiResult.Findings
        };
    }
}
