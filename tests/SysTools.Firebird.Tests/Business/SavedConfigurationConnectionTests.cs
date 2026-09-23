using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Configuration;
using SysTools.Business.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Business;

public sealed class SavedConfigurationConnectionTests
{
    [Fact]
    public async Task Loaded_configuration_reaches_connection_service_with_recovered_password_unchanged()
    {
        const string recoveredPassword = "Systools-Recovered-Secret-7284";
        var storedConfiguration = FirebirdTestConfiguration.Valid(recoveredPassword);
        var repository = new RecoveredConfigurationRepository(storedConfiguration);
        IConfigurationService configurationService = new ConfigurationService(
            repository,
            new ConfigurationValidator(),
            NullLogger<ConfigurationService>.Instance);
        var probe = new FakeFirebirdConnectionProbe();
        IConnectionTestService connectionService = new ConnectionTestService(
            probe,
            new ListLogger<ConnectionTestService>());

        var loadResult = await configurationService.LoadAsync();
        var loadedConfiguration = Assert.IsType<AppConfiguration>(loadResult.Configuration);
        var connectionResult = await connectionService.TestAsync(loadedConfiguration);

        Assert.Equal(ConfigurationLoadStatus.Loaded, loadResult.Status);
        Assert.Equal(ConnectionTestStatus.Success, connectionResult.Status);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Same(loadedConfiguration, probe.LastConfiguration);
        Assert.Equal(recoveredPassword, loadedConfiguration.Password);
        Assert.Equal(recoveredPassword, probe.LastConfiguration!.Password);
    }

    private sealed class RecoveredConfigurationRepository(AppConfiguration configuration)
        : IConfigurationRepository
    {
        public int ReadCalls { get; private set; }

        public Task<ConfigurationRepositoryReadResult> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            return Task.FromResult(new ConfigurationRepositoryReadResult(
                ConfigurationRepositoryReadStatus.Found,
                configuration));
        }

        public Task<ConfigurationRepositoryWriteResult> WriteAsync(
            AppConfiguration configurationToWrite,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("A recovered configuration must not be rewritten during load.");
    }
}
