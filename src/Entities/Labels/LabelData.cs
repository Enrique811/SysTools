using SysTools.Entities.Products;

namespace SysTools.Entities.Labels;

public sealed class LabelData
{
    public LabelData(Product product, string formattedPrice, string? information)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (product.PriceWithTax is null)
        {
            throw new ArgumentException(
                "No se puede preparar una etiqueta para un producto sin precio final.",
                nameof(product));
        }

        if (string.IsNullOrWhiteSpace(formattedPrice))
        {
            throw new ArgumentException("El precio formateado es obligatorio.", nameof(formattedPrice));
        }

        Product = product;
        FormattedPrice = formattedPrice;
        Information = information ?? string.Empty;
    }

    public Product Product { get; }

    public string FormattedPrice { get; }

    public string Information { get; }

    public override string ToString() => nameof(LabelData);
}
