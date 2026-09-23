using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SysTools.Business.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Presentation.Shell.Views;

namespace SysTools.Presentation.Tests.Composition;

public sealed class FirebirdFailureIsolationTests
{
    [Fact]
    public void Every_controlled_failure_leaves_the_shell_resolvable_and_usable()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var probe = new ControllableProbe();
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.ClearProviders());
                App.ConfigureServices(services);
                services.AddSingleton<IFirebirdConnectionProbe>(probe);

                using var provider = services.BuildServiceProvider();
                var service = provider.GetRequiredService<IConnectionTestService>();

                var invalidResult = service.TestAsync(new AppConfiguration())
                    .GetAwaiter()
                    .GetResult();
                Assert.Equal(ConnectionTestStatus.InvalidConfiguration, invalidResult.Status);

                foreach (var status in new[]
                {
                    ConnectionTestStatus.AuthenticationFailed,
                    ConnectionTestStatus.ServerUnavailable,
                    ConnectionTestStatus.DatabaseUnavailable,
                    ConnectionTestStatus.Timeout,
                    ConnectionTestStatus.Canceled,
                    ConnectionTestStatus.UnexpectedFailure
                })
                {
                    probe.Status = status;
                    var result = service.TestAsync(ValidConfiguration())
                        .GetAwaiter()
                        .GetResult();
                    Assert.Equal(status, result.Status);
                }

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

    private static AppConfiguration ValidConfiguration() => new()
    {
        IpEmpresa = "example.invalid",
        RutaEmpresa = "database.fdb",
        Usuario = "test-user",
        Password = "test-password"
    };

    private sealed class ControllableProbe : IFirebirdConnectionProbe
    {
        public ConnectionTestStatus Status { get; set; }

        public Task<ConnectionTestStatus> ProbeAsync(
            AppConfiguration configuration,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Status);
    }
}
