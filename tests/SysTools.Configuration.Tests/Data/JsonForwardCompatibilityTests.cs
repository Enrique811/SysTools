using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class JsonForwardCompatibilityTests
{
    [Fact]
    public async Task UnknownPropertiesAndHigherSchemaVersion_SurviveKnownFieldUpdate()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        await File.WriteAllTextAsync(path, """
            {
              "schemaVersion": 7,
              "ipEmpresa": "server",
              "rutaEmpresa": "database.fdb",
              "usuario": "SYSDBA",
              "passwordProtegido": "",
              "ambiente": "a",
              "impresora": "",
              "formatoPrecio": "MX",
              "reporte": "",
              "columnas": 1,
              "informacion": "before",
              "licencia": "",
              "futureSettings": { "enabled": true, "level": 4 }
            }
            """);
        var repository = new JsonConfigurationRepository(
            new FakePathProvider(path),
            new FakeSecretProtector(),
            new AtomicFileWriter(),
            NullLogger<JsonConfigurationRepository>.Instance);

        var read = await repository.ReadAsync();
        Assert.Equal(ConfigurationRepositoryReadStatus.Found, read.Status);
        var configuration = Assert.IsType<SysTools.Entities.Configuration.AppConfiguration>(read.Configuration);
        configuration = new SysTools.Entities.Configuration.AppConfiguration
        {
            IpEmpresa = configuration.IpEmpresa,
            RutaEmpresa = configuration.RutaEmpresa,
            Usuario = configuration.Usuario,
            Password = configuration.Password,
            Ambiente = configuration.Ambiente,
            Impresora = configuration.Impresora,
            FormatoPrecio = configuration.FormatoPrecio,
            Reporte = configuration.Reporte,
            Columnas = configuration.Columnas,
            Informacion = "after",
            Licencia = configuration.Licencia
        };

        var write = await repository.WriteAsync(configuration);

        Assert.Equal(ConfigurationRepositoryWriteStatus.Saved, write.Status);
        using var output = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Equal(7, output.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.True(output.RootElement.GetProperty("futureSettings").GetProperty("enabled").GetBoolean());
        Assert.Equal(4, output.RootElement.GetProperty("futureSettings").GetProperty("level").GetInt32());
        Assert.Equal("after", output.RootElement.GetProperty("informacion").GetString());
    }
}
