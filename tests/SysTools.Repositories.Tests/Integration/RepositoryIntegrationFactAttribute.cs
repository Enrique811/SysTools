using SysTools.Entities.Configuration;

namespace SysTools.Repositories.Tests.Integration;

internal sealed class RepositoryIntegrationFactAttribute : FactAttribute
{
    internal const string EnableVariable = "SYSTOOLS_FIREBIRD_RUN_REPOSITORY_INTEGRATION";

    public RepositoryIntegrationFactAttribute()
    {
        if (!TryGetEnvironment(out _, out _, out _))
        {
            Skip = "Repository integration environment is not configured.";
        }
    }

    internal static bool TryGetEnvironment(
        out AppConfiguration configuration,
        out string productCode,
        out string productPrefix)
    {
        var enabled = Environment.GetEnvironmentVariable(EnableVariable);
        var host = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_HOST");
        var database = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_DATABASE");
        var user = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_USER");
        var password = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_PASSWORD");
        productCode = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_PRODUCT_CODE") ?? string.Empty;
        productPrefix = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_PRODUCT_PREFIX") ?? string.Empty;

        var ready = string.Equals(enabled, "1", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(host)
            && !string.IsNullOrWhiteSpace(database)
            && !string.IsNullOrWhiteSpace(user)
            && !string.IsNullOrWhiteSpace(password);

        configuration = ready
            ? new AppConfiguration
            {
                IpEmpresa = host!,
                RutaEmpresa = database!,
                Usuario = user!,
                Password = password!
            }
            : AppConfiguration.CreateDefault();
        return ready;
    }
}
