namespace AI.FinancialKnowledgeCopilot.Application.Options;

public sealed class RelevanceOptions
{
    public const string SectionName = "Relevance";
    public bool Enabled { get; set; } = true;
    public int MinKeywordMatches { get; set; } = 1;
    public string[] FinancialKeywords { get; set; } =
    [
        // Markets & instruments
        "stock", "stocks", "share", "shares", "equity", "equities",
        "bond", "bonds", "gilt", "gilts", "etf", "fund", "funds",
        "index", "indices", "futures", "options", "derivative", "commodity",
        "forex", "currency", "crypto", "cryptocurrency",

        // Corporate finance
        "revenue", "profit", "earnings", "ebitda", "ebit", "margin", "margins",
        "dividend", "dividends", "yield", "eps", "p/e", "valuation",
        "balance sheet", "cash flow", "income statement", "annual report",
        "quarterly", "q1", "q2", "q3", "q4", "fiscal", "ipo",
        "acquisition", "merger", "takeover", "buyback",

        // Investment & portfolio
        "invest", "investment", "portfolio", "asset", "allocation",
        "return", "returns", "performance", "benchmark", "alpha", "beta",
        "volatility", "risk", "sharpe", "drawdown", "nav",

        // Banking & personal finance
        "bank", "banking", "loan", "mortgage", "interest rate", "inflation",
        "pension", "annuity", "isa", "sipp", "savings", "deposit",
        "credit", "debit", "debt", "leverage", "capital",

        // Regulation & macro
        "fca", "sec", "basel", "solvency", "gdp", "cpi", "rpi",
        "monetary policy", "quantitative easing", "central bank",
        "bank of england", "federal reserve", "ecb",

        // General financial terms
        "financial", "finance", "market", "markets", "trading", "trader",
        "analyst", "forecast", "projection", "outlook", "guidance",
        "report", "results", "quarter", "annual", "fiscal year"
    ];

    public string OffTopicResponse { get; set; } =
        "This assistant is designed to answer questions about financial topics only. " +
        "Please ask a question related to finance, investments, markets, or financial documents.";
}