using SysTools.Business.Labels;

namespace SysTools.BusinessRules.Tests.Pricing;

public sealed class PriceFormatterMxTests
{
    [Theory]
    [InlineData("0", "$0.00")]
    [InlineData("1", "$1.00")]
    [InlineData("123.4", "$123.40")]
    [InlineData("123.45", "$123.45")]
    [InlineData("-1.2", "$-1.20")]
    [InlineData("999999999.99", "$999999999.99")]
    [InlineData("0.004", "$0.00")]
    [InlineData("0.006", "$0.01")]
    [InlineData("12.345", "$12.34")]
    [InlineData("12.355", "$12.36")]
    public void Mx_uses_dollar_dot_and_exactly_two_decimals(string input, string expected)
    {
        var formatter = new PriceFormatterService();

        var actual = formatter.Format(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), "MX");

        Assert.Equal(expected, actual);
    }
}
