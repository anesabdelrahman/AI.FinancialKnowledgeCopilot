using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeOutputSafetyFilter : IOutputSafetyFilter
{
    public SafetyFilterResult Apply(string response)
    {
        return new SafetyFilterResult
        {
            FilteredResponse = $"{response}  - scrubbedd",
            PiiWasRedacted = true,
            RedactedItems = new List<PiiMatch>()
        };
    }
}
