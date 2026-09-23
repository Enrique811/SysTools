using Microsoft.Extensions.DependencyInjection;
using SysTools.Business.Configuration;
using SysTools.Data.Configuration;
using SysTools.Presentation;

namespace SysTools.Configuration.Tests.Composition;

public sealed class ConfigurationDependencyInjectionTests
{
    [Fact]
    public void ConfigureServices_ResolvesConfigurationServiceWithoutReadingAppData()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        App.ConfigureServices(services);
        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<IConfigurationService>();
        Assert.IsType<ConfigurationService>(service);
        Assert.IsType<JsonConfigurationRepository>(provider.GetRequiredService<IConfigurationRepository>());
    }
}
