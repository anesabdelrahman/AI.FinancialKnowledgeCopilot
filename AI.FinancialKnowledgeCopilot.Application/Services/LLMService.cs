using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

public class LLMService : ILLMService
{
    public Task<string> GenerateAnswerAsync(string question, IEnumerable<string> context, CancellationToken cancellationToken)
    {
        //TODO: Need real LLM call (OpenAI, Azure OpenAI, Anthropic, etc.).
        return Task.FromResult( $"Your question: {question} matches those answers: {string.Join(',', context)}");
    }
}