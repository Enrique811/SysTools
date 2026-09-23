using SysTools.Business.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Business;

public sealed class ConnectionTestServiceFailureTests
{
    [Fact]
    public async Task Test_returns_all_missing_fields_without_invoking_probe()
    {
        var configuration = new AppConfiguration
        {
            IpEmpresa = " ",
            RutaEmpresa = "",
            Usuario = "\t",
            Password = ""
        };
        var probe = new FakeFirebirdConnectionProbe();
        var logger = new ListLogger<ConnectionTestService>();
        var service = new ConnectionTestService(
            probe,
            new ConnectionConfigurationValidator(),
            logger);

        var result = await service.TestAsync(configuration);

        Assert.Equal(ConnectionTestStatus.InvalidConfiguration, result.Status);
        Assert.Equal(
            "Completa los datos requeridos para conectar con Firebird.",
            result.Message);
        Assert.Equal(
            ["ipEmpresa", "rutaEmpresa", "usuario", "password"],
            result.Issues.Select(issue => issue.Field));
        Assert.Equal(0, probe.Calls);
        Assert.Contains(logger.Entries, entry =>
            entry.Message.Contains("ConnectionValidationFailed", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ConnectionTestStatus.AuthenticationFailed, "Firebird rechazó las credenciales proporcionadas.")]
    [InlineData(ConnectionTestStatus.ServerUnavailable, "No fue posible contactar al servidor Firebird.")]
    [InlineData(ConnectionTestStatus.DatabaseUnavailable, "El servidor respondió, pero la base de datos no está disponible.")]
    [InlineData(ConnectionTestStatus.Timeout, "La conexión excedió el tiempo permitido.")]
    [InlineData(ConnectionTestStatus.Canceled, "La prueba de conexión fue cancelada.")]
    [InlineData(ConnectionTestStatus.UnexpectedFailure, "No fue posible comprobar la conexión con Firebird.")]
    public async Task Test_returns_safe_catalog_message_and_preserves_configuration(
        ConnectionTestStatus status,
        string expectedMessage)
    {
        var configuration = FirebirdTestConfiguration.Valid("  preserve exact password  ");
        var probe = new FakeFirebirdConnectionProbe
        {
            Behavior = (_, _) => Task.FromResult(status)
        };
        var service = new ConnectionTestService(
            probe,
            new ConnectionConfigurationValidator(),
            new ListLogger<ConnectionTestService>());

        var result = await service.TestAsync(configuration);

        Assert.Equal(status, result.Status);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Empty(result.Issues);
        Assert.Equal("  preserve exact password  ", configuration.Password);
        Assert.Same(configuration, probe.LastConfiguration);
    }
}
