using System.Globalization;
using ChatApp.StockBot;
using ChatApp.StockBot.Stooq;

namespace ChatApp.Tests;

public class QuoteMessageFormatterTests
{
    [Fact]
    public void Format_Success_MatchesChallengeFormatExactly()
    {
        var result = StooqQuoteResult.Success("AAPL.US", 93.42m);

        Assert.Equal("AAPL.US quote is $93.42 per share",
            QuoteMessageFormatter.Format(result, "aapl.us"));
    }

    [Fact]
    public void Format_Success_UppercasesSymbol()
    {
        var result = StooqQuoteResult.Success("aapl.us", 93.42m);

        Assert.StartsWith("AAPL.US quote is", QuoteMessageFormatter.Format(result, "aapl.us"));
    }

    [Theory]
    [InlineData(93.4, "$93.40")]
    [InlineData(93, "$93.00")]
    [InlineData(1234.567, "$1234.57")]
    public void Format_Success_AlwaysUsesTwoDecimals(decimal price, string expectedFragment)
    {
        var message = QuoteMessageFormatter.Format(StooqQuoteResult.Success("AAPL.US", price), "aapl.us");

        Assert.Contains(expectedFragment, message);
    }

    [Fact]
    public void Format_Success_UnderCommaDecimalCulture_KeepsDotSeparator()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            var message = QuoteMessageFormatter.Format(StooqQuoteResult.Success("AAPL.US", 93.42m), "aapl.us");

            Assert.Equal("AAPL.US quote is $93.42 per share", message);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Format_NotFound_MentionsRequestedCode()
    {
        var message = QuoteMessageFormatter.Format(StooqQuoteResult.NotFound("FAKE123"), "fake123");

        Assert.Contains("FAKE123", message);
        Assert.DoesNotContain("N/D", message);
    }

    [Fact]
    public void Format_Malformed_ReturnsFriendlyMessage()
    {
        var message = QuoteMessageFormatter.Format(StooqQuoteResult.Malformed(), "aapl.us");

        Assert.Contains("AAPL.US", message);
        Assert.Contains("Could not read", message);
    }

    [Fact]
    public void ServiceUnavailable_MentionsRequestedCode()
    {
        var message = QuoteMessageFormatter.ServiceUnavailable("aapl.us");

        Assert.Contains("AAPL.US", message);
        Assert.Contains("not responding", message);
    }
}
