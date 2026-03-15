namespace AI.FinancialKnowledgeCopilot.Application.Options;

public sealed class DisclaimerOptions
{
    public const string SectionName = "Disclaimer";

    /// <summary>When false, disclaimer injection is disabled entirely (e.g. internal tooling).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Keywords that trigger disclaimer injection.
    /// If any appear in the response the disclaimer is appended.
    /// </summary>
    public string[] TriggerKeywords { get; set; } =
    [
        "invest", "investment", "portfolio", "return", "returns", "recommend",
        "recommendation", "buy", "sell", "fund", "dividend", "yield", "equity",
        "bond", "shares", "stock", "pension", "annuity", "risk", "forecast",
        "projection", "growth", "performance", "asset", "allocation"
    ];

    /// <summary>The disclaimer text appended to triggered responses.</summary>
    public string Text { get; set; } =
        "\n\n---\n⚠️ **Important:** This information is provided for general knowledge purposes only " +
        "and does not constitute financial advice. Past performance is not a reliable indicator of " +
        "future results. Please consult a qualified financial adviser before making any investment decisions.";
}