namespace AI.FinancialKnowledgeCopilot.Application.Dto;

/// <summary>
/// Describes what PII was found and what the scrubbed text looks like.
/// </summary>
public sealed record PiiDetectionResult
{
    /// <summary>Whether any PII was detected in the original text.</summary>
    public bool HasPii => Findings.Count > 0;

    /// <summary>The text with all PII replaced by placeholder tokens.</summary>
    public string ScrubbedText { get; init; } = string.Empty;

    /// <summary>Each individual PII match that was found.</summary>
    public IReadOnlyList<PiiMatch> Findings { get; init; } = [];
}

public sealed record PiiMatch(PiiType Type, string Placeholder);

public enum PiiType
{
    EmailAddress,
    UkPhoneNumber,
    UkNationalInsuranceNumber,
    UkSortCode,
    UkBankAccountNumber,
    UkPostcode,
    UkIban,
    CreditCardNumber,
}
