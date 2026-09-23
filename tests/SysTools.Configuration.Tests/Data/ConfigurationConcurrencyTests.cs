using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class ConfigurationConcurrencyTests
{
    [Fact]
    public async Task NearbyWrites_AreSerializedAndLeaveOneCompleteDocument()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var repository = new JsonConfigurationRepository(
            new FakePathProvider(path),
            new FakeSecretProtector(),
            new AtomicFileWriter(),
            NullLogger<JsonConfigurationRepository>.Instance);
        var requests = Enumerable.Range(0, 10)
            .Select(index => repository.WriteAsync(new AppConfiguration
            {
                IpEmpresa = $"server-{index}",
                RutaEmpresa = $"database-{index}.fdb",
                Password = $"password-{index}"
            }));

        var results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.Equal(ConfigurationRepositoryWriteStatus.Saved, result.Status));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.StartsWith("server-", json.RootElement.GetProperty("ipEmpresa").GetString());
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp", SearchOption.TopDirectoryOnly));
    }
}
