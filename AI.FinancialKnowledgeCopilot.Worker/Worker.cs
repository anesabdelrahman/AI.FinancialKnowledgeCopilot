using AI.FinancialKnowledgeCopilot.Application;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace AI.FinancialKnowledgeCopilot.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<IngestionOptions> options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IngestionOptions _options = options.Value;
    private readonly HashSet<string> _ingestedFiles = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ingestion worker started. Source: {Path}", _options.SourcePath);

        ValidateOptions();

        do
        {
            await RunIngestionPassAsync(stoppingToken);

            if (_options.PollingIntervalSeconds > 0)
            {
                logger.LogInformation(
                    "Next scan in {Interval}s. Press Ctrl+C to stop.",
                    _options.PollingIntervalSeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
        }
        while (_options.PollingIntervalSeconds > 0 && !stoppingToken.IsCancellationRequested);

        logger.LogInformation("Ingestion worker stopped.");
    }

    // -------------------------------------------------------------------------

    private async Task RunIngestionPassAsync(CancellationToken stoppingToken)
    {
        var files = DiscoverFiles();

        if (files.Length == 0)
        {
            logger.LogWarning("No files found in {Path} matching {Extensions}",
                _options.SourcePath,
                string.Join(", ", _options.FileExtensions));
            return;
        }

        logger.LogInformation("Found {Count} file(s) to process.", files.Length);

        // IDocumentIngestionService may be Scoped — resolve per pass
        using var scope = scopeFactory.CreateScope();
        var ingestionService = scope.ServiceProvider
            .GetRequiredService<IDocumentIngestionService>();

        int succeeded = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var file in files)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (_options.SkipAlreadyIngested && _ingestedFiles.Contains(file))
            {
                logger.LogDebug("Skipping already-ingested file: {File}", file);
                skipped++;
                continue;
            }

            try
            {
                await IngestFileAsync(ingestionService, file, stoppingToken);
                _ingestedFiles.Add(file);
                succeeded++;
            }
            catch (Exception ex)
            {
                // Log and continue — one bad file should not halt the entire pass
                logger.LogError(ex, "Failed to ingest file: {File}", file);
                failed++;
            }
        }

        logger.LogInformation(
            "Ingestion pass complete. Succeeded: {Succeeded}, Skipped: {Skipped}, Failed: {Failed}",
            succeeded, skipped, failed);
    }

    private async Task IngestFileAsync(
        IDocumentIngestionService ingestionService,
        string filePath,
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Ingesting: {File}", filePath);

        var content = await File.ReadAllTextAsync(filePath, stoppingToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("Skipping empty file: {File}", filePath);
            return;
        }

        var title = Path.GetFileNameWithoutExtension(filePath);
        await ingestionService.IngestAsync(title, content, stoppingToken);

        logger.LogInformation("Successfully ingested: {Title}", title);
    }

    private string[] DiscoverFiles()
    {
        if (!Directory.Exists(_options.SourcePath))
        {
            logger.LogError("Source directory does not exist: {Path}", _options.SourcePath);
            return [];
        }

        return Directory
            .EnumerateFiles(_options.SourcePath, "*", SearchOption.AllDirectories)
            .Where(f => _options.FileExtensions.Contains(
                Path.GetExtension(f),
                StringComparer.OrdinalIgnoreCase))
            .Order()
            .ToArray();
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.SourcePath))
            throw new InvalidOperationException(
                $"'{IngestionOptions.SectionName}:{nameof(IngestionOptions.SourcePath)}' " +
                "is required but was not configured.");
    }
}
