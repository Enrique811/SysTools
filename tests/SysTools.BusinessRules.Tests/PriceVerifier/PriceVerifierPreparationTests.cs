using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.PriceVerifier;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierPreparationTests
{
    [Fact]
    public async Task Valid_dependencies_prepare_in_order_and_create_ready_session()
    {
        var order = new List<string>();
        var configuration = new ConfigurationServiceStub { CallOrder = order };
        configuration.LoadHandler = _ => Task.FromResult(PriceVerifierTestData.Loaded());
        var connection = new ConnectionTestServiceStub { CallOrder = order };
        connection.Handler = (_, _) => Task.FromResult(PriceVerifierTestData.Connected());
        var license = new LicenseServiceStub { CallOrder = order };
        license.Handler = (_, _, _) => Task.FromResult(PriceVerifierTestData.ValidLicense());

        var result = await Create(configuration, connection, license).PrepareAsync();

        Assert.True(result.IsReady);
        Assert.Equal(["Configuration", "Connection", "License"], order);
        Assert.Equal("Gracias por su preferencia", result.AdditionalInformation);
    }

    [Theory]
    [InlineData(ConfigurationLoadStatus.DefaultCreated)]
    [InlineData(ConfigurationLoadStatus.InvalidContent)]
    [InlineData(ConfigurationLoadStatus.SecretUnavailable)]
    [InlineData(ConfigurationLoadStatus.StorageFailure)]
    [InlineData(ConfigurationLoadStatus.AccessDenied)]
    [InlineData(ConfigurationLoadStatus.Canceled)]
    public async Task Every_non_loaded_configuration_status_short_circuits(ConfigurationLoadStatus status)
    {
        var configuration = new ConfigurationServiceStub();
        configuration.LoadHandler = _ => Task.FromResult(new ConfigurationLoadResult(status));
        var connection = new ConnectionTestServiceStub();
        var license = new LicenseServiceStub();

        var result = await Create(configuration, connection, license).PrepareAsync();

        Assert.Equal(PriceVerifierPreparationStatus.ConfigurationUnavailable, result.Status);
        Assert.Equal(0, connection.Calls);
        Assert.Equal(0, license.Calls);
    }

    [Theory]
    [InlineData(ConnectionTestStatus.InvalidConfiguration)]
    [InlineData(ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(ConnectionTestStatus.ServerUnavailable)]
    [InlineData(ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(ConnectionTestStatus.Timeout)]
    [InlineData(ConnectionTestStatus.Canceled)]
    [InlineData(ConnectionTestStatus.UnexpectedFailure)]
    public async Task Every_non_success_connection_status_short_circuits_license(ConnectionTestStatus status)
    {
        var connection = Connected();
        connection.Handler = (_, _) => Task.FromResult(new ConnectionTestResult(
            status,
            "No disponible.",
            TimeSpan.Zero,
            status == ConnectionTestStatus.InvalidConfiguration
                ? [new ConnectionConfigurationIssue("ipEmpresa", "No disponible.")]
                : null));
        var license = new LicenseServiceStub();

        var result = await Create(ReadyConfiguration(), connection, license).PrepareAsync();

        Assert.Equal(PriceVerifierPreparationStatus.ConnectionUnavailable, result.Status);
        Assert.Equal(status, result.ConnectionStatus);
        Assert.Equal(0, license.Calls);
    }

    [Theory]
    [InlineData(LicenseValidationStatus.MissingInput)]
    [InlineData(LicenseValidationStatus.SourceUnavailable)]
    [InlineData(LicenseValidationStatus.InvalidJson)]
    [InlineData(LicenseValidationStatus.InvalidFields)]
    [InlineData(LicenseValidationStatus.InvalidSignature)]
    [InlineData(LicenseValidationStatus.HardwareIdUnavailable)]
    [InlineData(LicenseValidationStatus.HardwareMismatch)]
    [InlineData(LicenseValidationStatus.NotYetValid)]
    [InlineData(LicenseValidationStatus.Expired)]
    [InlineData(LicenseValidationStatus.ServerTimeUnavailable)]
    public async Task Every_non_valid_license_status_blocks(LicenseValidationStatus status)
    {
        var license = new LicenseServiceStub();
        license.Handler = (_, _, _) => Task.FromResult(new LicenseValidationResult(status));

        var result = await Create(ReadyConfiguration(), Connected(), license).PrepareAsync();

        Assert.Equal(PriceVerifierPreparationStatus.LicenseUnavailable, result.Status);
        Assert.Equal(status, result.LicenseStatus);
    }

    [Fact]
    public async Task Loaded_configuration_that_is_not_connection_ready_short_circuits()
    {
        var configuration = new ConfigurationServiceStub
        {
            LoadHandler = _ => Task.FromResult(new ConfigurationLoadResult(
                ConfigurationLoadStatus.Loaded,
                PriceVerifierTestData.Configuration(),
                new ConfigurationValidationResult(false, false)))
        };
        var connection = new ConnectionTestServiceStub();
        var license = new LicenseServiceStub();

        var result = await Create(configuration, connection, license).PrepareAsync();

        Assert.Equal(PriceVerifierPreparationStatus.ConfigurationUnavailable, result.Status);
        Assert.Equal(0, connection.Calls);
        Assert.Equal(0, license.Calls);
    }

    private static ConfigurationServiceStub ReadyConfiguration()
    {
        var service = new ConfigurationServiceStub();
        service.LoadHandler = _ => Task.FromResult(PriceVerifierTestData.Loaded());
        return service;
    }

    private static ConnectionTestServiceStub Connected()
    {
        var service = new ConnectionTestServiceStub();
        service.Handler = (_, _) => Task.FromResult(PriceVerifierTestData.Connected());
        return service;
    }

    private static PriceVerifierWorkflow Create(
        ConfigurationServiceStub configuration,
        ConnectionTestServiceStub connection,
        LicenseServiceStub license) => new(
            configuration,
            connection,
            license,
            new ProductServiceStub(),
            new PriceFormatterServiceStub(),
            NullLogger<PriceVerifierWorkflow>.Instance);
}
