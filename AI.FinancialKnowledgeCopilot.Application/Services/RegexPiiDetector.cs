using System.Text.RegularExpressions;
using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Interfaces;

namespace AI.FinancialKnowledgeCopilot.Application.Services;

/// <summary>
/// Regex-based PII detector covering UK financial identifiers.
/// Patterns covered: email, UK phone, NI number, sort code, bank account,
/// postcode, IBAN (GB), and credit/debit card numbers.
/// </summary>
public sealed partial class RegexPiiDetector : IPiiDetector
{
    // -----------------------------------------------------------------
    // Pattern definitions — order matters: more specific patterns first
    // to avoid partial matches being swallowed by broader ones.
    // -----------------------------------------------------------------

    private static readonly IReadOnlyList<PiiPattern> Patterns =
    [
        // GB IBAN  e.g. GB29 NWBK 6016 1331 9268 19
        new(PiiType.UkIban,
            UkIbanRegex(),
            "[UK-IBAN REDACTED]"),

        // Credit / debit card — 13-19 digits, optional spaces/dashes between groups
        // Luhn validation is intentionally omitted here; false-positives filtered by context
        new(PiiType.CreditCardNumber,
            CreditCardRegex(),
            "[CARD-NUMBER REDACTED]"),

        // NI number  e.g. AB 12 34 56 C  (with or without spaces)
        new(PiiType.UkNationalInsuranceNumber,
            NiNumberRegex(),
            "[NI-NUMBER REDACTED]"),

        // Sort code  e.g. 12-34-56 or 12 34 56
        new(PiiType.UkSortCode,
            SortCodeRegex(),
            "[SORT-CODE REDACTED]"),

        // 8-digit bank account (only when preceded by account-related keywords
        // to avoid colliding with other 8-digit numbers)
        new(PiiType.UkBankAccountNumber,
            BankAccountRegex(),
            "[ACCOUNT-NUMBER REDACTED]"),

        // UK postcode  e.g. SW1A 1AA, CF10 1EP
        new(PiiType.UkPostcode,
            UkPostcodeRegex(),
            "[POSTCODE REDACTED]"),

        // UK mobile / landline  e.g. 07700 900000, +44 7700 900000, 020 1234 5678
        new(PiiType.UkPhoneNumber,
            UkPhoneRegex(),
            "[PHONE-NUMBER REDACTED]"),

        // Email address
        new(PiiType.EmailAddress,
            EmailRegex(),
            "[EMAIL REDACTED]"),
    ];

    // -----------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------

    public PiiDetectionResult Scrub(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new PiiDetectionResult { ScrubbedText = text };

        var findings = new List<PiiMatch>();
        var scrubbed = text;

        foreach (var pattern in Patterns)
        {
            scrubbed = pattern.Regex.Replace(scrubbed, match =>
            {
                findings.Add(new PiiMatch(pattern.Type, pattern.Placeholder));
                return pattern.Placeholder;
            });
        }

        return new PiiDetectionResult
        {
            ScrubbedText = scrubbed,
            Findings = findings
        };
    }

    public bool ContainsPii(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return Patterns.Any(p => p.Regex.IsMatch(text));
    }

    // -----------------------------------------------------------------
    // Source-generated compiled regexes
    // -----------------------------------------------------------------

    [GeneratedRegex(@"\bGB\d{2}[A-Z]{4}\d{14}\b",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex UkIbanRegex();

    [GeneratedRegex(@"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|6(?:011|5[0-9]{2})[0-9]{12})(?:[-\s]?[0-9]{4}){0,2}\b",
        RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b[A-CEGHJ-PR-TW-Z]{2}\s?\d{2}\s?\d{2}\s?\d{2}\s?[A-D]\b",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex NiNumberRegex();

    [GeneratedRegex(@"\b\d{2}[-\s]\d{2}[-\s]\d{2}\b",
        RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex SortCodeRegex();

    // Require account-number keywords nearby to reduce false positives
    [GeneratedRegex(@"(?i)(?:account\s*(?:number|no\.?|#)\s*:?\s*)\b\d{8}\b",
        RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex BankAccountRegex();

    [GeneratedRegex(@"\b[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}\b",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex UkPostcodeRegex();

    [GeneratedRegex(@"(?:(?:\+44\s?|0)(?:7\d{3}|\d{2,4})[\s-]?\d{3,4}[\s-]?\d{3,4})\b",
        RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex UkPhoneRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex EmailRegex();

    // -----------------------------------------------------------------
    // Internal helper
    // -----------------------------------------------------------------

    private sealed record PiiPattern(PiiType Type, Regex Regex, string Placeholder);
}
