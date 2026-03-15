using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

/// <summary>
/// Writes structured audit events using the standard <see cref="ILogger"/> pipeline,
/// allowing them to be routed to any configured sink (Application Insights, Seq,
/// Elasticsearch, etc.) without additional dependencies.
///
/// Each event is logged at Information level with a dedicated EventId so it can
/// be filtered and stored separately from general application logs.
/// </summary>
public sealed class StructuredAuditLogger(ILogger<StructuredAuditLogger> logger) : IAuditLogger
{
    // Dedicated EventId allows log sinks to route audit events to a separate store.
    private static readonly EventId AuditEventId = new(1000, "FinancialQueryAudit");

    public void LogQuery(AuditQueryEvent auditEvent)
    {
        // Structured logging — each property is individually queryable in log sinks.
        logger.Log(
            LogLevel.Information,
            AuditEventId,
            "AUDIT | CorrelationId={CorrelationId} Timestamp={Timestamp} " +
            "TopScore={TopConfidenceScore:F3} LowConfidence={WasLowConfidence} " +
            "InputPiiRedactions={InputPiiRedactionCount} OutputPiiRedactions={OutputPiiRedactionCount} " +
            "DisclaimerAppended={DisclaimerAppended} SourceCount={SourceCount} " +
            "Query={ScrubbedQuery} Response={ScrubbedResponse} Sources={Sources}",
            auditEvent.CorrelationId,
            auditEvent.Timestamp,
            auditEvent.TopConfidenceScore,
            auditEvent.WasLowConfidence,
            auditEvent.InputPiiRedactionCount,
            auditEvent.OutputPiiRedactionCount,
            auditEvent.DisclaimerAppended,
            auditEvent.Sources.Count,
            auditEvent.ScrubbedQuery,
            auditEvent.ScrubbedResponse,
            string.Join(", ", auditEvent.Sources));
    }
}