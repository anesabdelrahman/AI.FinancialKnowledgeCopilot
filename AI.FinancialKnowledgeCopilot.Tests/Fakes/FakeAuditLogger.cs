using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeAuditLogger : IAuditLogger
{
    public List<AuditQueryEvent> LoggedEvents { get; } = [];
    public void LogQuery(AuditQueryEvent auditEvent) => LoggedEvents.Add(auditEvent);
}
