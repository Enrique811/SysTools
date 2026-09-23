using Microsoft.Extensions.DependencyInjection;
using SysTools.Business.Labels;
using SysTools.Business.Products;

namespace SysTools.Presentation.Tests.Composition;

public sealed class BusinessRulesDependencyInjectionTests
{
    [Fact]
    public void Composition_resolves_business_rules_as_singletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        App.ConfigureServices(services);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        AssertSingleton<IProductService, ProductService>(provider);
        AssertSingleton<IPriceFormatterService, PriceFormatterService>(provider);
        AssertSingleton<ILabelQueueService, LabelQueueService>(provider);
    }

    private static void AssertSingleton<TService, TImplementation>(IServiceProvider provider)
        where TService : class
        where TImplementation : class, TService
    {
        var first = provider.GetRequiredService<TService>();
        var second = provider.GetRequiredService<TService>();

        Assert.IsType<TImplementation>(first);
        Assert.Same(first, second);
    }
}
