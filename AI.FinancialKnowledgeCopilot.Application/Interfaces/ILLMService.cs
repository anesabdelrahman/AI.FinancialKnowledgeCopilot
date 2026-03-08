namespace AI.FinancialKnowledgeCopilot.Application.Interfaces;

public interface ILLMService
{
    Task<string> GenerateAnswerAsync(string question, IEnumerable<string> context, CancellationToken cancellationToken);
}
