using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakePiiDetector : IPiiDetector
{
    public bool ContainsPii(string text)
    {
        return text.Length > 0;
    }

    public PiiDetectionResult Scrub(string text)
    {
        return new PiiDetectionResult
        {
            Findings = new List<PiiMatch>(),
            ScrubbedText = $"{text} - scrubbed"
        };
    }
}
