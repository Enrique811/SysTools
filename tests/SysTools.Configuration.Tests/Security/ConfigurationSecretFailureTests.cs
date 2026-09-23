using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Security;

public sealed class ConfigurationSecretFailureTests
{
    [Fact]
    [Trait("Category", "Security")]
    public async Task InvalidBase64_IsSecretUnavailableWithoutPartialConfiguration()
    {
        using var temporary = new TemporaryDirectory();
        await File.WriteAllTextAsync(temporary.File(), ValidJson("not-valid-base64***"));
        var repository = CreateRepository(temporary.File(), new FakeSecretProtector());

        var result = await repository.ReadAsync();

        Assert.Equal(ConfigurationRepositoryReadStatus.SecretUnavailable, result.Status);
        Assert.Null(result.Configuration);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task ProtectorFailure_IsSecretUnavailableWithoutChangingSource()
    {
        using var temporary = new TemporaryDirectory();
        var protectedValue = Convert.ToBase64String("protected:value"u8.ToArray());
        var json = ValidJson(protectedValue);
        await File.WriteAllTextAsync(temporary.File(), json);
        var protector = new FakeSecretProtector { ThrowOnUnprotect = true };
        var repository = CreateRepository(temporary.File(), protector);

        var result = await repository.ReadAsync();

        Assert.Equal(ConfigurationRepositoryReadStatus.SecretUnavailable, result.Status);
        Assert.Null(result.Configuration);
        Assert.Equal(json, await File.ReadAllTextAsync(temporary.File()));
    }

    private static JsonConfigurationRepository CreateRepository(string path, ISecretProtector protector) => new(
        new FakePathProvider(path),
        protector,
        new AtomicFileWriter(),
        NullLogger<JsonConfigurationRepository>.Instance);

    private static string ValidJson(string protectedPassword) => $$"""
        {
          "schemaVersion": 1,
          "ipEmpresa": "server",
          "rutaEmpresa": "database.fdb",
          "usuario": "SYSDBA",
          "passwordProtegido": "{{protectedPassword}}",
          "ambiente": "a",
          "impresora": "",
          "formatoPrecio": "MX",
          "reporte": "",
          "columnas": 1,
          "informacion": "",
          "licencia": ""
        }
        """;
}
