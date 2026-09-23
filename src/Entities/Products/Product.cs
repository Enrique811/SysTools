namespace SysTools.Entities.Products;

public sealed class Product
{
    public const string StockNotRegistered = "Sin registro";

    public Product(
        int id,
        string barcode,
        string? description,
        string? presentation,
        decimal? priceWithTax,
        string? stock)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

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

        Id = id;
        Barcode = barcode;
        Description = description;
        Presentation = presentation;
        PriceWithTax = priceWithTax;
        Stock = string.IsNullOrWhiteSpace(stock) ? StockNotRegistered : stock;
    }

    public int Id { get; }

    public string Barcode { get; }

    public string Description { get; }

    public string? Presentation { get; }

    public decimal? PriceWithTax { get; }

    public string Stock { get; }

    public override string ToString() => $"{nameof(Product)} {{ Id = {Id} }}";
}
