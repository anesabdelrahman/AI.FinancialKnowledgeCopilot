namespace AI.FinancialKnowledgeCopilot.Worker;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public string SourcePath { get; set; } = string.Empty;
    public string[] FileExtensions { get; set; } = [".txt", ".md"];
    public int PollingIntervalSeconds { get; set; } = 60;
    public bool SkipAlreadyIngested { get; set; } = true;
}
