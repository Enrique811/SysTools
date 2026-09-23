using System.Globalization;
using SysTools.Business.Labels;

namespace SysTools.BusinessRules.Tests.Pricing;

public sealed class PriceFormatterCultureTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("es-MX")]
    [InlineData("es-CO")]
    public void Output_is_independent_from_current_cultures(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var formatter = new PriceFormatterService();

            Assert.Equal("$1234.56", formatter.Format(1234.56m, "MX"));
            Assert.Equal("$ 1.235", formatter.Format(1234.56m, "CO"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData("2.5", 2)]
    [InlineData("3.5", 4)]
    [InlineData("-2.5", -2)]
    [InlineData("-3.5", -4)]
    public void Colombian_midpoints_use_to_even(string input, int expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        var formatter = new PriceFormatterService();
        var expectedText = expected.ToString("C0", CultureInfo.GetCultureInfo("es-CO"));

        Assert.Equal(expectedText, formatter.Format(value, "CO"));
    }
}
