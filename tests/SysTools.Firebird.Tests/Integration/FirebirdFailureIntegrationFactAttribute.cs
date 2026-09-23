namespace SysTools.Firebird.Tests.Integration;

internal sealed class FirebirdFailureIntegrationFactAttribute : FactAttribute
{
    public FirebirdFailureIntegrationFactAttribute()
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
            "SYSTOOLS_FIREBIRD_TEST_PASSWORD",
            "SYSTOOLS_FIREBIRD_TEST_BAD_PASSWORD",
            "SYSTOOLS_FIREBIRD_TEST_MISSING_DATABASE"
        };
        if (requiredVariables.Any(name =>
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))))
        {
            Skip = "Authorized Firebird failure integration variables are incomplete.";
        }
    }
}
