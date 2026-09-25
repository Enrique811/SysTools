using SysTools.Business.Configuration;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Security;

public sealed class ConfigurationLoggingTests
{
    [Fact]
    [Trait("Category", "Security")]
    public async Task ServiceAndRepositoryLogs_ContainOutcomesButNoConfigurationValues()
    {
        const string password = "Systools-Test-Secret-Logging";
        const string license = "Systools-Test-License-Logging";
        using var temporary = new TemporaryDirectory();
        var repositoryLogger = new ListLogger<JsonConfigurationRepository>();
        var repository = new JsonConfigurationRepository(
            new FakePathProvider(temporary.File()),
            new FakeSecretProtector(),
            new AtomicFileWriter(),
            repositoryLogger);
        var serviceLogger = new ListLogger<ConfigurationService>();
        var service = new ConfigurationService(repository, new ConfigurationValidator(), serviceLogger);
        var configuration = new AppConfiguration
        {
            IpEmpresa = "sensitive-host",
            RutaEmpresa = "sensitive-database-path",
            Usuario = "sensitive-user",
            Password = password,
            Licencia = license
        };

        var save = await service.SaveAsync(configuration);
        var load = await service.LoadAsync();
        var logText = string.Join(Environment.NewLine, repositoryLogger.Messages.Concat(serviceLogger.Messages));

        Assert.Equal(ConfigurationSaveStatus.Saved, save.Status);
        Assert.Equal(ConfigurationLoadStatus.Loaded, load.Status);
        Assert.Contains("Outcome", logText, StringComparison.Ordinal);
        Assert.DoesNotContain(password, logText, StringComparison.Ordinal);
        Assert.DoesNotContain(license, logText, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-host", logText, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-database-path", logText, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-user", logText, StringComparison.Ordinal);
        Assert.DoesNotContain("AppConfiguration", logText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task ValidationLog_ContainsOnlyIssueMetadata()
    {
        const string password = "Systools-Test-Invalid-Password";
        const string license = "Systools-Test-Invalid-License";
        var repository = new FakeConfigurationRepository();
        var logger = new ListLogger<ConfigurationService>();
        var service = new ConfigurationService(repository, new ConfigurationValidator(), logger);

        await service.SaveAsync(new AppConfiguration
        {
            Password = password,
            Licencia = license,
            Ambiente = "invalid"
        });
        var logText = string.Join(Environment.NewLine, logger.Messages);

        Assert.Contains("IssueCodes", logText, StringComparison.Ordinal);
        Assert.DoesNotContain(password, logText, StringComparison.Ordinal);
        Assert.DoesNotContain(license, logText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task Repository_failure_log_does_not_attach_exception_or_private_path()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File("PRIVATE_CONFIGURATION.json");
        await File.WriteAllTextAsync(path, "{invalid");
        var logger = new ListLogger<JsonConfigurationRepository>();
        var repository = new JsonConfigurationRepository(
            new FakePathProvider(path),
            new FakeSecretProtector(),
            new AtomicFileWriter(),
            logger);

        var result = await repository.ReadAsync();
        var logText = string.Join(Environment.NewLine, logger.Messages);

        Assert.Equal(ConfigurationRepositoryReadStatus.InvalidContent, result.Status);
        Assert.All(logger.Exceptions, Assert.Null);
        Assert.DoesNotContain("PRIVATE_CONFIGURATION", logText, StringComparison.Ordinal);
        Assert.Contains("JsonException", logText, StringComparison.Ordinal);
    }
}
