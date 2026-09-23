using SysTools.Business.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Business;

public sealed class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator = new();

    [Fact]
    public void Defaults_ArePersistableButReportEveryConnectionRequirementInStableOrder()
    {
        var result = _validator.Validate(AppConfiguration.CreateDefault());

        Assert.True(result.IsPersistable);
        Assert.False(result.IsConnectionReady);
        Assert.Equal(
            ["ipEmpresa", "rutaEmpresa", "password"],
            result.Issues.Select(issue => issue.Field));
        Assert.All(result.Issues, issue =>
        {
            Assert.Equal(ConfigurationIssueCode.RequiredForConnection, issue.Code);
            Assert.Equal(ConfigurationIssueSeverity.Warning, issue.Severity);
        });
    }

    [Fact]
    public void InvalidCatalogs_AndMissingConnectionFields_AreReturnedTogether()
    {
        var configuration = new AppConfiguration
        {
            Usuario = " ",
            Ambiente = "produccion",
            FormatoPrecio = "USD",
            Columnas = 4
        };

        var result = _validator.Validate(configuration);

        Assert.False(result.IsPersistable);
        Assert.False(result.IsConnectionReady);
        Assert.Equal(7, result.Issues.Count);
        Assert.Equal(
            ["ipEmpresa", "rutaEmpresa", "usuario", "password", "ambiente", "formatoPrecio", "columnas"],
            result.Issues.Select(issue => issue.Field));
    }

    [Fact]
    public void CompleteSupportedConfiguration_IsPersistableAndConnectionReady()
    {
        var result = _validator.Validate(new AppConfiguration
        {
            IpEmpresa = "127.0.0.1",
            RutaEmpresa = @"C:\Datos\empresa.fdb",
            Usuario = "SYSDBA",
            Password = "test-only",
            Ambiente = "b",
            FormatoPrecio = "CO",
            Columnas = 3
        });

        Assert.True(result.IsPersistable);
        Assert.True(result.IsConnectionReady);
        Assert.Empty(result.Issues);
    }
}
