using Microsoft.Extensions.DependencyInjection;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;

namespace SysTools.Presentation.Tests.Composition;

public sealed class BarcodeDependencyInjectionTests
{
    [Fact]
    public void Composition_resolves_barcode_dependencies_as_singletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        App.ConfigureServices(services);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var firstEncoder = provider.GetRequiredService<IBarcodeImageEncoder>();
        var secondEncoder = provider.GetRequiredService<IBarcodeImageEncoder>();
        Assert.IsType<ZxingBarcodeImageEncoder>(firstEncoder);
        Assert.Same(firstEncoder, secondEncoder);

        var firstService = provider.GetRequiredService<IBarcodeService>();
        var secondService = provider.GetRequiredService<IBarcodeService>();
        Assert.IsType<BarcodeService>(firstService);
        Assert.Same(firstService, secondService);
    }
}
