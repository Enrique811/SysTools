using SysTools.Business.Connection;
using SysTools.Entities.Configuration;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Business;

public sealed class ConnectionConfigurationValidatorTests
{
    [Fact]
    public void Validate_returns_every_missing_connection_field_in_stable_order()
    {
        var configuration = new AppConfiguration
        {
            IpEmpresa = " ",
            RutaEmpresa = "\t",
            Usuario = "",
            Password = "  ",
            Ambiente = "",
            FormatoPrecio = "",
            Licencia = ""
        };
        var validator = new ConnectionConfigurationValidator();

        var issues = validator.Validate(configuration);

        Assert.Equal(
            ["ipEmpresa", "rutaEmpresa", "usuario", "password"],
            issues.Select(issue => issue.Field));
        Assert.Equal(
            [
                "El servidor Firebird es obligatorio.",
                "La ruta de la base de datos es obligatoria.",
                "El usuario Firebird es obligatorio.",
                "La contraseña Firebird es obligatoria."
            ],
            issues.Select(issue => issue.Message));
    }

    [Fact]
    public void Validate_ignores_unrelated_catalog_fields_and_does_not_modify_values()
    {
        var configuration = FirebirdTestConfiguration.Valid("  exact secret  ");
        var originalValues = new[]
        {
            configuration.IpEmpresa,
            configuration.RutaEmpresa,
            configuration.Usuario,
            configuration.Password
        };
        var validator = new ConnectionConfigurationValidator();

        var issues = validator.Validate(configuration);

        Assert.Empty(issues);
        Assert.Equal(
            originalValues,
            new[]
            {
                configuration.IpEmpresa,
                configuration.RutaEmpresa,
                configuration.Usuario,
                configuration.Password
            });
    }
}
