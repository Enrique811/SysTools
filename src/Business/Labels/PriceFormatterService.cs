using System.Globalization;

namespace SysTools.Business.Labels;

public sealed class PriceFormatterService : IPriceFormatterService
{
    private static readonly CultureInfo ColombianCulture =
        CultureInfo.GetCultureInfo("es-CO");

    public string Format(decimal amount, string? formatCode)
    {
        if (string.Equals(formatCode, "MX", StringComparison.Ordinal))
        {
            var rounded = decimal.Round(amount, 2, MidpointRounding.ToEven);
            return "$" + rounded.ToString("0.00", CultureInfo.InvariantCulture);
        }

        var colombianAmount = decimal.Round(amount, 0, MidpointRounding.ToEven);
        return colombianAmount.ToString("C0", ColombianCulture);
    }
}
