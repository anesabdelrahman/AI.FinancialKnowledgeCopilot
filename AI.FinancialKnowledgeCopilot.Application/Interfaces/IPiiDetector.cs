using AI.FinancialKnowledgeCopilot.Application.Dto;

namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface IPiiDetector
{
    PiiDetectionResult Scrub(string text);
    bool ContainsPii(string text);
}
