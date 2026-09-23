using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class JsonConfigurationRepositoryRoundTripTests
{
    [Fact]
    public async Task TwentyConfigurations_RoundTripAllFunctionalFieldsWithoutPlaintextPassword()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var repository = CreateRepository(path);

        for (var index = 0; index < 20; index++)
        {
            var expected = CreateConfiguration(index);

            var write = await repository.WriteAsync(expected);
            var read = await repository.ReadAsync();
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(ConfigurationRepositoryWriteStatus.Saved, write.Status);
            Assert.Equal(ConfigurationRepositoryReadStatus.Found, read.Status);
            AssertConfiguration(expected, Assert.IsType<AppConfiguration>(read.Configuration));
            Assert.DoesNotContain(expected.Password, json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task StoredJson_UsesTheV1NamesAndProtectedPasswordOnly()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var repository = CreateRepository(path);
        var configuration = CreateConfiguration(1);

        await repository.WriteAsync(configuration);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(configuration.IpEmpresa, root.GetProperty("ipEmpresa").GetString());
        Assert.True(root.TryGetProperty("passwordProtegido", out var protectedPassword));
        Assert.False(string.IsNullOrWhiteSpace(protectedPassword.GetString()));
        Assert.False(root.TryGetProperty("password", out _));
    }

    private static JsonConfigurationRepository CreateRepository(string path) => new(
        new FakePathProvider(path),
        new FakeSecretProtector(),
        new AtomicFileWriter(),
        NullLogger<JsonConfigurationRepository>.Instance);

    private static AppConfiguration CreateConfiguration(int index) => new()
    {
        IpEmpresa = $"servidor-{index}",
        RutaEmpresa = $@"C:\Datos con acento\empresa-{index}-México.fdb",
        Usuario = $"usuario-{index}",
        Password = $"Systools-Test-Secret-{index}",
        Ambiente = index % 2 == 0 ? "a" : "b",
        Impresora = $"Impresora Ñ {index}",
        FormatoPrecio = index % 2 == 0 ? "MX" : "CO",
        Reporte = $"etiqueta-{index}.frx",
        Columnas = index % 3 + 1,
        Informacion = $"Información larga con acentos y espacios internos {index}",
        Licencia = $"licencia-opaca-{index}"
    };

    private static void AssertConfiguration(AppConfiguration expected, AppConfiguration actual)
    {
        Assert.Equal(expected.IpEmpresa, actual.IpEmpresa);
        Assert.Equal(expected.RutaEmpresa, actual.RutaEmpresa);
        Assert.Equal(expected.Usuario, actual.Usuario);
        Assert.Equal(expected.Password, actual.Password);
        Assert.Equal(expected.Ambiente, actual.Ambiente);
        Assert.Equal(expected.Impresora, actual.Impresora);
        Assert.Equal(expected.FormatoPrecio, actual.FormatoPrecio);
        Assert.Equal(expected.Reporte, actual.Reporte);
        Assert.Equal(expected.Columnas, actual.Columnas);
        Assert.Equal(expected.Informacion, actual.Informacion);
        Assert.Equal(expected.Licencia, actual.Licencia);
    }
}
