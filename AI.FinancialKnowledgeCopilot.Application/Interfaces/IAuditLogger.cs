using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

/// <summary>
/// Records a structured audit trail for every query processed by the system.
/// Implementations should write to a durable, tamper-evident store suitable
/// for FCA/regulatory compliance requirements.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a completed query event with all information needed for a compliance audit.
    /// All text parameters must already be PII-scrubbed before being passed here.
    /// </summary>
    void LogQuery(AuditQueryEvent auditEvent);
}
