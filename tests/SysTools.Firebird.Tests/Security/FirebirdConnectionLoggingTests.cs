using SysTools.Business.Connection;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Security;

public sealed class FirebirdConnectionLoggingTests
{
    private static readonly string[] Sentinels =
    [
        "SYSTOOLS-SECRET-PASSWORD-7851",
        "SYSTOOLS-LICENSE-SECRET-1942",
        "SYSTOOLS-HOST-SECRET-6330",
        "SYSTOOLS-PATH-SECRET-4077",
        "SYSTOOLS-USER-SECRET-5219",
        "SYSTOOLS-CONNECTION-STRING-SECRET-8604"
    ];

    [Fact]
    [Trait("Category", "Security")]
    public async Task Results_and_structured_logs_never_contain_configuration_or_raw_exception()
    {
        var configuration = new AppConfiguration
        {
            IpEmpresa = Sentinels[2],
            RutaEmpresa = Sentinels[3],
            Usuario = Sentinels[4],
            Password = Sentinels[0],
            Licencia = Sentinels[1]
        };
        var exceptionText = string.Join("|", Sentinels);
        var connection = new TrackingDbConnection
        {
            ConnectionString = Sentinels[5],
            OpenBehavior = _ => throw new InvalidOperationException(exceptionText)
        };
        var dataLogger = new ListLogger<FirebirdConnectionProbe>();
        var probe = new FirebirdConnectionProbe(
            new FakeFirebirdConnectionFactory(_ => connection),
            dataLogger,
            new FirebirdErrorClassifier(),
            TimeSpan.FromSeconds(1));
        var businessLogger = new ListLogger<ConnectionTestService>();
        var service = new ConnectionTestService(
            probe,
            new ConnectionConfigurationValidator(),
            businessLogger);

        var result = await service.TestAsync(configuration);

        Assert.Equal(ConnectionTestStatus.UnexpectedFailure, result.Status);
        Assert.Empty(result.Issues);
        var diagnosticText = string.Join(
            "|",
            dataLogger.Entries.Concat(businessLogger.Entries).SelectMany(entry =>
                new[]
                {
                    entry.Message,
                    entry.Exception?.ToString() ?? string.Empty,
                    string.Join(";", entry.Properties.Select(property =>
                        $"{property.Key}={property.Value}"))
                }).Append(result.Message));

        Assert.All(Sentinels, sentinel =>
            Assert.DoesNotContain(sentinel, diagnosticText, StringComparison.Ordinal));
        Assert.All(dataLogger.Entries.Concat(businessLogger.Entries), entry =>
            Assert.Null(entry.Exception));
        Assert.DoesNotContain(
            dataLogger.Entries.Concat(businessLogger.Entries)
                .SelectMany(entry => entry.Properties.Keys),
            key => key.Contains("Configuration", StringComparison.OrdinalIgnoreCase)
                || key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Host", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Path", StringComparison.OrdinalIgnoreCase)
                || key.Contains("User", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || key.Contains("ExceptionMessage", StringComparison.OrdinalIgnoreCase));
    }
}
