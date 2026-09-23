using Microsoft.Extensions.DependencyInjection;
using SysTools.Business.Configuration;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;
using SysTools.Presentation;
using SysTools.Presentation.Shell.Views;

namespace SysTools.Configuration.Tests.Composition;

public sealed class ConfigurationFailureIsolationTests
{
    [Fact]
    [Trait("Category", "Failure")]
    public void ConfigurationFailure_DoesNotPreventShellResolution()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddLogging();
                App.ConfigureServices(services);
                services.AddSingleton<IConfigurationPathProvider>(new FakePathProvider(" "));
                using var provider = services.BuildServiceProvider();

                var result = provider.GetRequiredService<IConfigurationService>().LoadAsync().GetAwaiter().GetResult();
                Assert.Equal(ConfigurationLoadStatus.StorageFailure, result.Status);
                var shell = provider.GetRequiredService<ShellWindow>();
                Assert.NotNull(shell.DataContext);
                shell.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
