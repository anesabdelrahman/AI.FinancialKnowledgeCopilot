using AI.FinancialKnowledgeCopilot.Application.Dto;
using AI.FinancialKnowledgeCopilot.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AI.FinancialKnowledgeCopilot.Tests;

// RegexPiiDetector

[TestFixture]
public class RegexPiiDetectorTests
{
    private RegexPiiDetector _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RegexPiiDetector();

    // --- Email ---

    [Test]
    public void Scrub_Email_IsRedacted()
    {
        var result = _sut.Scrub("Contact me at john.smith@example.com for details.");
        Assert.That(result.ScrubbedText, Does.Not.Contain("john.smith@example.com"));
        Assert.That(result.ScrubbedText, Does.Contain("[EMAIL REDACTED]"));
    }

    [Test]
    public void Scrub_Email_FindingTypeIsRecorded()
    {
        var result = _sut.Scrub("user@bank.co.uk");
        Assert.That(result.Findings, Has.Some.Matches<PiiMatch>(f => f.Type == PiiType.EmailAddress));
    }

    // --- NI Number ---

    [Test]
    [TestCase("My NI is AB 12 34 56 C.")]
    [TestCase("NI: AB123456C")]
    public void Scrub_NiNumber_IsRedacted(string input)
    {
        var result = _sut.Scrub(input);
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[NI-NUMBER REDACTED]"));
    }

    [Test]
    public void Scrub_NiNumber_FindingTypeIsRecorded()
    {
        var result = _sut.Scrub("NI: AB123456C");
        Assert.That(result.Findings, Has.Some.Matches<PiiMatch>(f => f.Type == PiiType.UkNationalInsuranceNumber));
    }

    // --- Sort Code ---

    [Test]
    [TestCase("Sort code is 12-34-56")]
    [TestCase("Sort code: 12 34 56")]
    public void Scrub_SortCode_IsRedacted(string input)
    {
        var result = _sut.Scrub(input);
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[SORT-CODE REDACTED]"));
    }

    // --- Bank Account ---

    [Test]
    public void Scrub_BankAccountWithKeyword_IsRedacted()
    {
        var result = _sut.Scrub("Account number: 12345678");
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[ACCOUNT-NUMBER REDACTED]"));
    }

    [Test]
    public void Scrub_StandaloneEightDigits_IsNotRedacted()
    {
        // Without an account-related keyword, 8 digits alone should not trigger
        var result = _sut.Scrub("The year was 19831234 and sales were strong.");
        Assert.That(result.Findings, Has.None.Matches<PiiMatch>(f => f.Type == PiiType.UkBankAccountNumber));
    }

    // --- UK Postcode ---

    [Test]
    [TestCase("I live in CF10 1EP.")]
    [TestCase("Postcode: SW1A 1AA")]
    public void Scrub_UkPostcode_IsRedacted(string input)
    {
        var result = _sut.Scrub(input);
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[POSTCODE REDACTED]"));
    }

    // --- UK Phone ---

    [Test]
    [TestCase("Call me on 07700 900000")]
    [TestCase("Phone: +44 7700 900000")]
    public void Scrub_UkPhoneNumber_IsRedacted(string input)
    {
        var result = _sut.Scrub(input);
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[PHONE-NUMBER REDACTED]"));
    }

    // --- UK IBAN ---

    [Test]
    public void Scrub_UkIban_IsRedacted()
    {
        var result = _sut.Scrub("My IBAN is GB29NWBK60161331926819.");
        Assert.That(result.HasPii, Is.True);
        Assert.That(result.ScrubbedText, Does.Contain("[UK-IBAN REDACTED]"));
    }

    // --- Multiple PII types ---

    [Test]
    public void Scrub_MultiplePiiTypes_AllRedacted()
    {
        const string input = "Email user@test.com, NI AB123456C, sort code 12-34-56.";
        var result = _sut.Scrub(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.ScrubbedText, Does.Not.Contain("user@test.com"));
            Assert.That(result.ScrubbedText, Does.Not.Contain("AB123456C"));
            Assert.That(result.ScrubbedText, Does.Not.Contain("12-34-56"));
            Assert.That(result.Findings, Has.Count.GreaterThanOrEqualTo(3));
        });
    }

    // --- Clean text ---

    [Test]
    public void Scrub_NoPii_ReturnsOriginalText()
    {
        const string input = "Apple reported strong Q3 earnings with revenue up 8%.";
        var result = _sut.Scrub(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasPii, Is.False);
            Assert.That(result.ScrubbedText, Is.EqualTo(input));
            Assert.That(result.Findings, Is.Empty);
        });
    }

    [Test]
    public void Scrub_EmptyString_ReturnsEmptyResult()
    {
        var result = _sut.Scrub(string.Empty);
        Assert.That(result.HasPii, Is.False);
    }

    // --- ContainsPii ---

    [Test]
    public void ContainsPii_WithPii_ReturnsTrue()
        => Assert.That(_sut.ContainsPii("user@example.com"), Is.True);

    [Test]
    public void ContainsPii_WithoutPii_ReturnsFalse()
        => Assert.That(_sut.ContainsPii("Quarterly revenue was up 12%."), Is.False);
}


// OutputSafetyFilter


[TestFixture]
public class OutputSafetyFilterTests
{
    private RegexPiiDetector _piiDetector = null!;
    private OutputSafetyFilter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _piiDetector = new RegexPiiDetector();
        _sut = new OutputSafetyFilter(_piiDetector, NullLogger<OutputSafetyFilter>.Instance);
    }

    [Test]
    public void Apply_ResponseWithPii_PiiIsRedacted()
    {
        var result = _sut.Apply("The client email is jane.doe@wealth.com and NI AB123456C.");

        Assert.Multiple(() =>
        {
            Assert.That(result.FilteredResponse, Does.Not.Contain("jane.doe@wealth.com"));
            Assert.That(result.FilteredResponse, Does.Not.Contain("AB123456C"));
            Assert.That(result.PiiWasRedacted, Is.True);
        });
    }

    [Test]
    public void Apply_CleanResponse_IsUnchanged()
    {
        const string clean = "Revenue increased by 15% year on year.";
        var result = _sut.Apply(clean);

        Assert.Multiple(() =>
        {
            Assert.That(result.FilteredResponse, Is.EqualTo(clean));
            Assert.That(result.PiiWasRedacted, Is.False);
            Assert.That(result.RedactedItems, Is.Empty);
        });
    }

    [Test]
    public void Apply_ResponseWithPii_RedactedItemsAreRecorded()
    {
        var result = _sut.Apply("Contact admin@example.com or call 07700 900000.");

        Assert.That(result.RedactedItems, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Apply_EmptyResponse_ReturnsEmptyFilteredResponse()
    {
        var result = _sut.Apply(string.Empty);
        Assert.That(result.FilteredResponse, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Apply_NullResponse_DoesNotThrow()
        => Assert.DoesNotThrow(() => _sut.Apply(null!));
}


// QueryService — PII guardrail integration


[TestFixture]
public class QueryServicePiiGuardrailTests
{
    private FakeEmbeddingService _embeddingService = null!;
    private FakeVectorStore _vectorStore = null!;
    private FakeLLMService _llmService = null!;
    private QueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _embeddingService = new FakeEmbeddingService();
        _vectorStore = new FakeVectorStore();
        _llmService = new FakeLLMService();

        var piiDetector = new RegexPiiDetector();
        var safetyFilter = new OutputSafetyFilter(piiDetector, NullLogger<OutputSafetyFilter>.Instance);

        _sut = new QueryService(
            _embeddingService,
            _vectorStore,
            _llmService,
            piiDetector,
            safetyFilter,
            NullLogger<QueryService>.Instance);
    }

    [Test]
    public async Task AskAsync_QueryContainsPii_PiiIsStrippedBeforeEmbedding()
    {
        var request = new QueryRequest { Query = "What accounts does user@test.com have?" };

        await _sut.AskAsync(request, CancellationToken.None);

        Assert.That(_embeddingService.ReceivedInputs.Single(), Does.Not.Contain("user@test.com"));
    }

    [Test]
    public async Task AskAsync_QueryContainsPii_PiiIsStrippedBeforeLLMCall()
    {
        var request = new QueryRequest { Query = "Check NI AB123456C portfolio" };

        await _sut.AskAsync(request, CancellationToken.None);

        Assert.That(_llmService.ReceivedQuestion, Does.Not.Contain("AB123456C"));
    }

    [Test]
    public async Task AskAsync_LlmResponseContainsPii_PiiIsRedactedInAnswer()
    {
        _llmService.AnswerToReturn = "The account holder is reachable at ceo@company.com";

        var result = await _sut.AskAsync(new QueryRequest { Query = "Who manages this fund?" }, CancellationToken.None);

        Assert.That(result.Answer, Does.Not.Contain("ceo@company.com"));
        Assert.That(result.Answer, Does.Contain("[EMAIL REDACTED]"));
    }

    [Test]
    public async Task AskAsync_CleanQueryAndResponse_PassesThroughUnchanged()
    {
        _llmService.AnswerToReturn = "Revenue grew 12% in Q3.";

        var result = await _sut.AskAsync(
            new QueryRequest { Query = "What was Q3 revenue growth?" },
            CancellationToken.None);

        Assert.That(result.Answer, Is.EqualTo("Revenue grew 12% in Q3."));
    }
}
