namespace SysTools.Firebird.Tests.Integration;

internal sealed class FirebirdIntegrationFactAttribute : FactAttribute
{
    public FirebirdIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_RUN_INTEGRATION"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Firebird integration is disabled.";
            return;
        }

        var requiredVariables = new[]
        {
            "SYSTOOLS_FIREBIRD_TEST_HOST",
            "SYSTOOLS_FIREBIRD_TEST_DATABASE",
            "SYSTOOLS_FIREBIRD_TEST_USER",
            "SYSTOOLS_FIREBIRD_TEST_PASSWORD"
        };
        if (requiredVariables.Any(name =>
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))))
        {
            Skip = "Firebird integration variables are incomplete.";
        }
    }
}
