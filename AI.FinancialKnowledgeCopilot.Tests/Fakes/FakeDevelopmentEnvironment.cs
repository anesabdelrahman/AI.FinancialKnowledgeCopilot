using Microsoft.Extensions.FileProviders;

namespace AI.FinancialKnowledgeCopilot.Tests.Fakes;

// ---------------------------------------------------------------------------
// FakeDevelopmentEnvironment (needed for InMemoryVectorStore)
// ---------------------------------------------------------------------------

internal class FakeDevelopmentEnvironment //: Microsoft.Extensions.Hosting.IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}