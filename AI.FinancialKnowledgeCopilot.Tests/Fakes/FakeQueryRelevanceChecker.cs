using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeQueryRelevanceChecker : IQueryRelevanceChecker
{
    public RelevanceCheckResult Check(string query)
    {
        return new RelevanceCheckResult { IsRelevant = true };
    }
}
