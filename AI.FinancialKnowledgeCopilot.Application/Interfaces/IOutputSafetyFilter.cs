using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface IOutputSafetyFilter
{
    SafetyFilterResult Apply(string response);
}
