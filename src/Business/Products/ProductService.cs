using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.Business.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<Product?> GetByBarcodeAsync(
        AppConfiguration configuration,
        string? barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return Task.FromResult<Product?>(null);
        }

        return _repository.GetByBarcodeAsync(
            configuration,
            barcode.Trim(),
            cancellationToken);
    }

    public Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
        AppConfiguration configuration,
        string? descriptionPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(descriptionPrefix))
        {
            return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
        }

        return _repository.SearchByDescriptionAsync(
            configuration,
            descriptionPrefix.Trim(),
            cancellationToken);
    }
}
