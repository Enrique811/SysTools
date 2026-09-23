using SysTools.Business.Labels;

namespace SysTools.BusinessRules.Tests.Pricing;

public sealed class PriceFormatterCoTests
{
    [Theory]
    [InlineData("0", "CO", "$ 0")]
    [InlineData("123.4", "CO", "$ 123")]
    [InlineData("123.5", "CO", "$ 124")]
    [InlineData("124.5", "CO", "$ 124")]
    [InlineData("1234.5", "CO", "$ 1.234")]
    [InlineData("-123.5", "CO", "-$ 124")]
    [InlineData("10", null, "$ 10")]
    [InlineData("10", "", "$ 10")]
    [InlineData("10", "mx", "$ 10")]
    [InlineData("10", "USD", "$ 10")]
    public void Co_and_unknown_codes_use_legacy_colombian_fallback(
        string input,
        string? formatCode,
        string expected)
    {
        var formatter = new PriceFormatterService();

        var actual = formatter.Format(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), formatCode);

        Assert.Equal(expected, actual);
    }
}
