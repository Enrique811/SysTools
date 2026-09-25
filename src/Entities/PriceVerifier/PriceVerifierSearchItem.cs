using SysTools.Entities.Products;

namespace SysTools.Entities.PriceVerifier;

public sealed class PriceVerifierSearchItem
{
    public PriceVerifierSearchItem(string barcode, string? description, string? formattedPrice, string? stock)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("El codigo de barras es obligatorio.", nameof(barcode));
        }

        if (barcode.Length > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(barcode));
        }

        description ??= string.Empty;
        if (description.Length > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(description));
        }

        Barcode = barcode;
        Description = description;
        FormattedPrice = formattedPrice;
        Stock = string.IsNullOrWhiteSpace(stock) ? Product.StockNotRegistered : stock;
    }

    public string Barcode { get; }
    public string Description { get; }
    public string? FormattedPrice { get; }
    public string Stock { get; }
}
