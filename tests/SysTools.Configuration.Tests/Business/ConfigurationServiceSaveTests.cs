using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Configuration;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Business;

public sealed class ConfigurationServiceSaveTests
{
    [Fact]
    public async Task InvalidCatalogs_AreRejectedWithoutRepositoryIo()
    {
        var repository = new FakeConfigurationRepository();
        var service = CreateService(repository);
        var configuration = new AppConfiguration { Ambiente = "invalid" };

        var result = await service.SaveAsync(configuration);

        Assert.Equal(ConfigurationSaveStatus.ValidationFailed, result.Status);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task IncompleteConnectionData_CanStillBePersisted()
    {
        var repository = new FakeConfigurationRepository();
        var service = CreateService(repository);

        var result = await service.SaveAsync(AppConfiguration.CreateDefault());

        Assert.Equal(ConfigurationSaveStatus.Saved, result.Status);
        Assert.True(result.Validation.IsPersistable);
        Assert.False(result.Validation.IsConnectionReady);
        Assert.Equal(1, repository.WriteCalls);
    }

    [Fact]
    public async Task RepositoryFailure_IsTranslatedToTypedResult()
    {
        var repository = new FakeConfigurationRepository
        {
            WriteResult = new ConfigurationRepositoryWriteResult(ConfigurationRepositoryWriteStatus.ProtectionFailure)
        };
        var service = CreateService(repository);

        var result = await service.SaveAsync(AppConfiguration.CreateDefault());

        Assert.Equal(ConfigurationSaveStatus.ProtectionFailure, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == ConfigurationIssueCode.SecretUnavailable);
    }

    private static ConfigurationService CreateService(FakeConfigurationRepository repository) =>
        new(repository, new ConfigurationValidator(), NullLogger<ConfigurationService>.Instance);
}
