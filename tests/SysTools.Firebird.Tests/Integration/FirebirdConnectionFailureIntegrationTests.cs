using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Firebird.Tests.Integration;

public sealed class FirebirdConnectionFailureIntegrationTests
{
    [Trait("Category", "FirebirdIntegration")]
    [Trait("Category", "FirebirdFailureIntegration")]
    [FirebirdFailureIntegrationFact]
    public async Task Invalid_password_returns_authentication_or_conservative_fallback()
    {
        var configuration = LoadConfiguration(
            "SYSTOOLS_FIREBIRD_TEST_BAD_PASSWORD",
            "SYSTOOLS_FIREBIRD_TEST_DATABASE");

        var status = await CreateProbe().ProbeAsync(configuration);

        Assert.Contains(
            status,
            new[]
            {
                ConnectionTestStatus.AuthenticationFailed,
                ConnectionTestStatus.UnexpectedFailure
            });
    }

    [Trait("Category", "FirebirdIntegration")]
    [Trait("Category", "FirebirdFailureIntegration")]
    [FirebirdFailureIntegrationFact]
    public async Task Missing_database_returns_database_unavailable_or_conservative_fallback()
    {
        var configuration = LoadConfiguration(
            "SYSTOOLS_FIREBIRD_TEST_PASSWORD",
            "SYSTOOLS_FIREBIRD_TEST_MISSING_DATABASE");

        var status = await CreateProbe().ProbeAsync(configuration);

        Assert.Contains(
            status,
            new[]
            {
                ConnectionTestStatus.DatabaseUnavailable,
                ConnectionTestStatus.UnexpectedFailure
            });
    }

    private static FirebirdConnectionProbe CreateProbe() =>
        new(
            new FirebirdConnectionFactory(),
            NullLogger<FirebirdConnectionProbe>.Instance);

    private static AppConfiguration LoadConfiguration(
        string passwordVariable,
        string databaseVariable)
    {
        return new AppConfiguration
        {
            IpEmpresa = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_HOST")!,
            RutaEmpresa = Environment.GetEnvironmentVariable(databaseVariable)!,
            Usuario = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_USER")!,
            Password = Environment.GetEnvironmentVariable(passwordVariable)!
        };
    }
}
