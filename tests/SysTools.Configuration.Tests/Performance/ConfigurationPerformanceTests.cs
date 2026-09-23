using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Configuration;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Performance;

public sealed class ConfigurationPerformanceTests
{
    [Fact]
    [Trait("Category", "Performance")]
    public async Task DefaultCreation_CompletesUnderOneSecondInAtLeastNinetyFiveOfOneHundredRuns()
    {
        var underOneSecond = 0;

        for (var iteration = 0; iteration < 100; iteration++)
        {
            using var temporary = new TemporaryDirectory();
            var repository = new JsonConfigurationRepository(
                new FakePathProvider(temporary.File()),
                new FakeSecretProtector(),
                new AtomicFileWriter(),
                NullLogger<JsonConfigurationRepository>.Instance);
            var service = new ConfigurationService(
                repository,
                new ConfigurationValidator(),
                NullLogger<ConfigurationService>.Instance);
            var stopwatch = Stopwatch.StartNew();

            var result = await service.LoadAsync();

            stopwatch.Stop();
            Assert.Equal(ConfigurationLoadStatus.DefaultCreated, result.Status);
            if (stopwatch.Elapsed < TimeSpan.FromSeconds(1)) underOneSecond++;
            Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp", SearchOption.TopDirectoryOnly));
        }

        Assert.True(
            underOneSecond >= 95,
            $"Expected at least 95 operations under one second, observed {underOneSecond}.");
    }
}
