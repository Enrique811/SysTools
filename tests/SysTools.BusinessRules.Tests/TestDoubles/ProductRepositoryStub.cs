using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.TestDoubles;

internal sealed class ProductRepositoryStub : IProductRepository
{
    internal Func<AppConfiguration, string, CancellationToken, Task<Product?>> GetByBarcodeHandler { get; set; } =
        static (_, _, _) => Task.FromResult<Product?>(null);

    internal Func<AppConfiguration, string?, CancellationToken, Task<IReadOnlyList<Product>>> SearchHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());

    internal int GetByBarcodeCalls { get; private set; }

    internal int SearchCalls { get; private set; }

    internal AppConfiguration? LastConfiguration { get; private set; }

    internal string? LastBarcode { get; private set; }

    internal string? LastDescriptionPrefix { get; private set; }

    internal CancellationToken LastCancellationToken { get; private set; }

    public Task<Product?> GetByBarcodeAsync(
        AppConfiguration configuration,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        GetByBarcodeCalls++;
        LastConfiguration = configuration;
        LastBarcode = barcode;
        LastCancellationToken = cancellationToken;
        return GetByBarcodeHandler(configuration, barcode, cancellationToken);
    }

    public Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
        AppConfiguration configuration,
        string? descriptionPrefix,
        CancellationToken cancellationToken = default)
    {
        SearchCalls++;
        LastConfiguration = configuration;
        LastDescriptionPrefix = descriptionPrefix;
        LastCancellationToken = cancellationToken;
        return SearchHandler(configuration, descriptionPrefix, cancellationToken);
    }
}
