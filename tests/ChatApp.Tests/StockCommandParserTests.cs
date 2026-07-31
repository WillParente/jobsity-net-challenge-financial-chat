using ChatApp.Web.Services;

namespace ChatApp.Tests;

public class StockCommandParserTests
{
    [Theory]
    [InlineData("/stock=aapl.us", "aapl.us")]
    [InlineData("/STOCK=AAPL.US", "aapl.us")]
    [InlineData("  /stock=aapl.us  ", "aapl.us")]
    [InlineData("/stock=MSFT.US", "msft.us")]
    public void Parse_ValidCommand_ReturnsNormalizedStockCode(string input, string expectedCode)
    {
        var result = StockCommandParser.Parse(input);

        Assert.Equal(CommandParseOutcome.StockCommand, result.Outcome);
        Assert.Equal(expectedCode, result.StockCode);
    }

    [Theory]
    [InlineData("/stock=")]
    [InlineData("/stock=   ")]
    [InlineData("/stock=aapl us")]
    public void Parse_StockCommandWithMissingOrInvalidCode_ReturnsInvalid(string input)
    {
        var result = StockCommandParser.Parse(input);

        Assert.Equal(CommandParseOutcome.InvalidStockCommand, result.Outcome);
        Assert.Null(result.StockCode);
    }

    [Theory]
    [InlineData("/help")]
    [InlineData("/stock aapl.us")]
    [InlineData("/stock =aapl.us")]
    [InlineData("/quote=aapl.us")]
    public void Parse_UnknownSlashCommand_ReturnsUnknownCommand(string input)
    {
        var result = StockCommandParser.Parse(input);

        Assert.Equal(CommandParseOutcome.UnknownCommand, result.Outcome);
    }

    [Theory]
    [InlineData("good morning")]
    [InlineData("check this: /stock=aapl.us")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_PlainMessageOrEmptyInput_ReturnsNotACommand(string? input)
    {
        var result = StockCommandParser.Parse(input);

        Assert.Equal(CommandParseOutcome.NotACommand, result.Outcome);
    }
}
