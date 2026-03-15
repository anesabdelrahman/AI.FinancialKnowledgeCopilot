using System;
using System.Linq;
using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using AI.FinancialKnowledgeCopilot.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public sealed class OutputSafetyFilter : IOutputSafetyFilter
{
    private readonly IPiiDetector _piiDetector;
    private readonly DisclaimerOptions _disclaimer;
    private readonly ILogger<OutputSafetyFilter> _logger;

    public OutputSafetyFilter(
        IPiiDetector piiDetector,
        IOptions<DisclaimerOptions>? disclaimerOptions,
        ILogger<OutputSafetyFilter> logger)
    {
        _piiDetector = piiDetector ?? throw new ArgumentNullException(nameof(piiDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Defensive: tests or callers may pass null IOptions or an IOptions whose Value is null.
        // Use a sensible default to avoid NREs and to make behavior deterministic in unit tests.
        _disclaimer = disclaimerOptions?.Value ?? new DisclaimerOptions();
    }

    public SafetyFilterResult Apply(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new SafetyFilterResult { FilteredResponse = response ?? string.Empty };

        // Step 1 — PII scrubbing
        var piiResult = _piiDetector.Scrub(response);

        if (piiResult.HasPii)
        {
            _logger.LogWarning(
                "PII redacted from LLM response. Types: {PiiTypes}",
                string.Join(", ", piiResult.Findings.Select(f => f.Type)));
        }

        // Step 2 — Financial disclaimer injection
        var (finalText, disclaimerAppended) = TryAppendDisclaimer(piiResult.ScrubbedText);

        if (disclaimerAppended)
            _logger.LogDebug("Financial disclaimer appended to response.");

        return new SafetyFilterResult
        {
            FilteredResponse = finalText,
            PiiWasRedacted = piiResult.HasPii,
            RedactedItems = piiResult.Findings,
            DisclaimerAppended = disclaimerAppended
        };
    }

    private (string text, bool appended) TryAppendDisclaimer(string text)
    {
        if (!_disclaimer.Enabled)
            return (text, false);

        bool triggered = _disclaimer.TriggerKeywords?.Any(keyword =>
            text.Contains(keyword ?? string.Empty, StringComparison.OrdinalIgnoreCase)) ?? false;

        return triggered
            ? (text + _disclaimer.Text, true)
            : (text, false);
    }
}