using Microsoft.Extensions.Options;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

public static class OptionsHelper
{
    public static IOptions<T> For<T>(T value) where T : class => Options.Create(value);
}
