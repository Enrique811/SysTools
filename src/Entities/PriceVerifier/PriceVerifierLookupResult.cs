using SysTools.Entities.Products;

namespace SysTools.Entities.PriceVerifier;

public sealed class PriceVerifierLookupResult
{
    public PriceVerifierLookupResult(
        PriceVerifierLookupStatus status,
        string message,
        Product? product = null,
        string? formattedPrice = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));
        }

        if (status == PriceVerifierLookupStatus.Success && product is null)
        {
            throw new ArgumentException("Una consulta exitosa requiere producto.", nameof(product));
        }

        if (status != PriceVerifierLookupStatus.Success && (product is not null || formattedPrice is not null))
        {
            throw new ArgumentException("Un resultado no exitoso no admite datos comerciales.");
        }

        if (formattedPrice is not null && product?.PriceWithTax is null)
        {
            throw new ArgumentException("El precio formateado requiere un precio de producto.", nameof(formattedPrice));
        }

        Status = status;
        Message = message;
        Product = product;
        FormattedPrice = formattedPrice;
    }

    public PriceVerifierLookupStatus Status { get; }
    public string Message { get; }
    public Product? Product { get; }
    public string? FormattedPrice { get; }
    public bool IsSuccess => Status == PriceVerifierLookupStatus.Success;
}

