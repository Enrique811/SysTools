using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Entities;

public sealed class AppConfigurationTests
{
    [Fact]
    public void CreateDefault_UsesSafeDocumentedValues()
    {
        var configuration = AppConfiguration.CreateDefault();

        Assert.Equal(string.Empty, configuration.IpEmpresa);
        Assert.Equal(string.Empty, configuration.RutaEmpresa);
        Assert.Equal("SYSDBA", configuration.Usuario);
        Assert.Equal(string.Empty, configuration.Password);
        Assert.Equal("a", configuration.Ambiente);
        Assert.Equal(string.Empty, configuration.Impresora);
        Assert.Equal("MX", configuration.FormatoPrecio);
        Assert.Equal(string.Empty, configuration.Reporte);
        Assert.Equal(1, configuration.Columnas);
        Assert.Equal(string.Empty, configuration.Informacion);
        Assert.Equal(string.Empty, configuration.Licencia);
    }

    [Fact]
    public void StringProperties_NormalizeNullAndToStringDoesNotExposeValues()
    {
        var configuration = new AppConfiguration
        {
            IpEmpresa = null!,
            RutaEmpresa = null!,
            Usuario = null!,
            Password = "sentinel-password",
            Ambiente = null!,
            Impresora = null!,
            FormatoPrecio = null!,
            Reporte = null!,
            Informacion = null!,
            Licencia = "sentinel-license"
        };

        Assert.Equal(string.Empty, configuration.IpEmpresa);
        Assert.Equal(string.Empty, configuration.Usuario);
        Assert.DoesNotContain("sentinel-password", configuration.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("sentinel-license", configuration.ToString(), StringComparison.Ordinal);
    }
}
