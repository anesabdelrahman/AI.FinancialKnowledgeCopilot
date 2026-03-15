using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

internal class FakeLLMService : ILLMService
{
    public string AnswerToReturn { get; set; } = "Fake LLM answer.";
    public string? ReceivedQuestion { get; private set; }
    public IEnumerable<string>? ReceivedContext { get; private set; }

    public Task<string> GenerateAnswerAsync(string question, IEnumerable<string> context, CancellationToken ct)
    {
        ReceivedQuestion = question;
        ReceivedContext = context;
        return Task.FromResult(AnswerToReturn);
    }
}
