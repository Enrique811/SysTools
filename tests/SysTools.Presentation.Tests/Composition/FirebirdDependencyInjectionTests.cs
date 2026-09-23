using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SysTools.Business.Connection;
using SysTools.Data.Connection;
using SysTools.Presentation.Shell.Views;

namespace SysTools.Presentation.Tests.Composition;

public sealed class FirebirdDependencyInjectionTests
{
    [Fact]
    public void Composition_resolves_firebird_services_and_shell_without_opening_network()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.ClearProviders());
                App.ConfigureServices(services);

                using var provider = services.BuildServiceProvider(
                    new ServiceProviderOptions
                    {
                        ValidateOnBuild = true,
                        ValidateScopes = true
                    });

                Assert.IsType<FirebirdConnectionFactory>(
                    provider.GetRequiredService<IFirebirdConnectionFactory>());
                Assert.IsType<FirebirdConnectionProbe>(
                    provider.GetRequiredService<IFirebirdConnectionProbe>());
                Assert.IsType<ConnectionTestService>(
                    provider.GetRequiredService<IConnectionTestService>());

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
