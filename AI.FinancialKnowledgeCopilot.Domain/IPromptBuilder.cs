
namespace AI.FinancialKnowledgeCopilot.Domain;

public interface IPromptBuilder
{
    object Build(string question, CancellationToken cancellationToken);
}
