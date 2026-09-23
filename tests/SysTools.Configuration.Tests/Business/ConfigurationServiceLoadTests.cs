using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Configuration;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Business;

public sealed class ConfigurationServiceLoadTests
{
    [Fact]
    public async Task MissingConfiguration_CreatesDefaultsAndReturnsReadinessIssues()
    {
        var repository = new FakeConfigurationRepository();
        var service = CreateService(repository);

        var result = await service.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.DefaultCreated, result.Status);
        Assert.NotNull(result.Configuration);
        Assert.Equal("SYSDBA", result.Configuration.Usuario);
        Assert.NotNull(result.Validation);
        Assert.True(result.Validation.IsPersistable);
        Assert.False(result.Validation.IsConnectionReady);
        Assert.Equal(3, result.Validation.Issues.Count);
        Assert.Equal(1, repository.WriteCalls);
    }

    [Fact]
    public async Task ExistingConfiguration_IsReturnedWithoutWriting()
    {
        var expected = new AppConfiguration { IpEmpresa = "server" };
        var repository = new FakeConfigurationRepository
        {
            ReadResult = new ConfigurationRepositoryReadResult(ConfigurationRepositoryReadStatus.Found, expected)
        };
        var service = CreateService(repository);

        var result = await service.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.Loaded, result.Status);
        Assert.Same(expected, result.Configuration);
        Assert.Equal(0, repository.WriteCalls);
    }

    [Fact]
    public async Task MissingConfiguration_WhenDefaultCannotBeWritten_ReturnsControlledFailure()
    {
        var repository = new FakeConfigurationRepository
        {
            WriteResult = new ConfigurationRepositoryWriteResult(ConfigurationRepositoryWriteStatus.StorageFailure)
        };
        var service = CreateService(repository);

        var result = await service.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.StorageFailure, result.Status);
        Assert.Null(result.Configuration);
        Assert.Contains(result.Issues, issue => issue.Code == ConfigurationIssueCode.StorageUnavailable);
    }

    private static ConfigurationService CreateService(FakeConfigurationRepository repository) =>
        new(repository, new ConfigurationValidator(), NullLogger<ConfigurationService>.Instance);
}
