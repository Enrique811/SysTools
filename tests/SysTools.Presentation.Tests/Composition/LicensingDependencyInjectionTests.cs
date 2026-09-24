using Microsoft.Extensions.DependencyInjection;
using SysTools.Business.Licensing;
using SysTools.Data.Licensing;

namespace SysTools.Presentation.Tests.Composition;

public sealed class LicensingDependencyInjectionTests
{
    [Fact]
    public void Composition_resolves_licensing_dependencies_as_singletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        App.ConfigureServices(services);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        AssertSingleton<ILicenseSourceReader, FileLicenseSourceReader>(provider);
        AssertSingleton<IHardwareIdProvider, WindowsHardwareIdProvider>(provider);
        AssertSingleton<ILicenseSignatureVerifier, LegacyLicenseSignatureVerifier>(provider);
        AssertSingleton<ILicenseService, LicenseService>(provider);
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
