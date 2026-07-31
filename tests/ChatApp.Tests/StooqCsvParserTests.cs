using System.Globalization;
using ChatApp.StockBot.Stooq;

namespace ChatApp.Tests;

public class StooqCsvParserTests
{
    private const string ValidCsv =
        "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
        "AAPL.US,2026-07-30,22:00:07,208.9,213.58,208.1,213.42,49500000\n";

    private const string NotFoundCsv =
        "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
        "FAKE123,N/D,N/D,N/D,N/D,N/D,N/D,N/D\n";

    [Fact]
    public void Parse_ValidCsv_ReturnsSymbolAndClosePrice()
    {
        var result = StooqCsvParser.Parse(ValidCsv);

        Assert.Equal(StooqQuoteStatus.Success, result.Status);
        Assert.Equal("AAPL.US", result.Symbol);
        Assert.Equal(213.42m, result.Price);
    }

    [Fact]
    public void Parse_WindowsLineEndings_StillParses()
    {
        var result = StooqCsvParser.Parse(ValidCsv.Replace("\n", "\r\n"));

        Assert.Equal(StooqQuoteStatus.Success, result.Status);
        Assert.Equal(213.42m, result.Price);
    }

    [Fact]
    public void Parse_ReorderedColumns_LocatesCloseByHeaderName()
    {
        const string reordered =
            "Close,Symbol,Date\n" +
            "93.42,AAPL.US,2026-07-30\n";

        var result = StooqCsvParser.Parse(reordered);

        Assert.Equal(StooqQuoteStatus.Success, result.Status);
        Assert.Equal("AAPL.US", result.Symbol);
        Assert.Equal(93.42m, result.Price);
    }

    [Fact]
    public void Parse_NotFoundTicker_ReturnsNotFound()
    {
        var result = StooqCsvParser.Parse(NotFoundCsv);

        Assert.Equal(StooqQuoteStatus.NotFound, result.Status);
        Assert.Equal("FAKE123", result.Symbol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyBody_ReturnsMalformed(string? csv)
    {
        Assert.Equal(StooqQuoteStatus.Malformed, StooqCsvParser.Parse(csv).Status);
    }

    [Fact]
    public void Parse_HeaderOnly_ReturnsMalformed()
    {
        const string headerOnly = "Symbol,Date,Time,Open,High,Low,Close,Volume\n";

        Assert.Equal(StooqQuoteStatus.Malformed, StooqCsvParser.Parse(headerOnly).Status);
    }

    [Fact]
    public void Parse_RowWithTooFewColumns_ReturnsMalformed()
    {
        const string shortRow =
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
            "AAPL.US,2026-07-30\n";

        Assert.Equal(StooqQuoteStatus.Malformed, StooqCsvParser.Parse(shortRow).Status);
    }

    [Fact]
    public void Parse_NonNumericPrice_ReturnsMalformed()
    {
        const string garbagePrice =
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
            "AAPL.US,2026-07-30,22:00:07,208.9,213.58,208.1,not-a-price,49500000\n";

        Assert.Equal(StooqQuoteStatus.Malformed, StooqCsvParser.Parse(garbagePrice).Status);
    }

    [Fact]
    public void Parse_UnderCommaDecimalCulture_UsesInvariantCulture()
    {
        // On a pt-BR machine "213.42" must not become 21342.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            var result = StooqCsvParser.Parse(ValidCsv);

            Assert.Equal(StooqQuoteStatus.Success, result.Status);
            Assert.Equal(213.42m, result.Price);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
