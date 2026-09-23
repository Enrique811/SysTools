using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.Business.Products;

public interface IProductService
{
    Task<Product?> GetByBarcodeAsync(
        AppConfiguration configuration,
        string? barcode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
        AppConfiguration configuration,
        string? descriptionPrefix,
        CancellationToken cancellationToken = default);
}
