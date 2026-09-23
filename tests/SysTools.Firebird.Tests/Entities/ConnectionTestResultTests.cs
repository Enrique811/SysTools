using SysTools.Entities.Connection;

namespace SysTools.Firebird.Tests.Entities;

public sealed class ConnectionTestResultTests
{
    [Fact]
    public void Status_catalog_is_stable_and_complete()
    {
        var names = Enum.GetNames<ConnectionTestStatus>();

        Assert.Equal(
            [
                "Success",
                "InvalidConfiguration",
                "AuthenticationFailed",
                "ServerUnavailable",
                "DatabaseUnavailable",
                "Timeout",
                "Canceled",
                "UnexpectedFailure"
            ],
            names);
    }

    [Theory]
    [InlineData(ConnectionTestStatus.Success)]
    [InlineData(ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(ConnectionTestStatus.ServerUnavailable)]
    [InlineData(ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(ConnectionTestStatus.Timeout)]
    [InlineData(ConnectionTestStatus.Canceled)]
    [InlineData(ConnectionTestStatus.UnexpectedFailure)]
    public void Non_validation_results_have_no_issues(ConnectionTestStatus status)
    {
        var result = new ConnectionTestResult(status, "Mensaje seguro.", TimeSpan.Zero);

        Assert.Empty(result.Issues);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public void Invalid_configuration_requires_at_least_one_safe_issue()
    {
        var issue = new ConnectionConfigurationIssue("password", "El campo password es necesario para conectar.");
        var result = new ConnectionTestResult(
            ConnectionTestStatus.InvalidConfiguration,
            "Completa los datos requeridos para conectar con Firebird.",
            TimeSpan.Zero,
            [issue]);

        Assert.Single(result.Issues);
        Assert.Throws<ArgumentException>(() => new ConnectionTestResult(
            ConnectionTestStatus.InvalidConfiguration,
            "Mensaje seguro.",
            TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_rejects_negative_duration_or_issues_on_other_statuses()
    {
        var issue = new ConnectionConfigurationIssue("usuario", "El campo usuario es necesario para conectar.");

        Assert.Throws<ArgumentOutOfRangeException>(() => new ConnectionTestResult(
            ConnectionTestStatus.Success,
            "Mensaje seguro.",
            TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentException>(() => new ConnectionTestResult(
            ConnectionTestStatus.Success,
            "Mensaje seguro.",
            TimeSpan.Zero,
            [issue]));
    }

    [Fact]
    public void Issue_rejects_unknown_fields_and_does_not_require_a_value()
    {
        var issue = new ConnectionConfigurationIssue("ipEmpresa", "El campo ipEmpresa es necesario para conectar.");

        Assert.Equal("ipEmpresa", issue.Field);
        Assert.DoesNotContain("192.0.2.10", issue.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => new ConnectionConfigurationIssue("licencia", "Mensaje seguro."));
    }

    [Fact]
    public void Issues_are_defensively_copied_and_cannot_be_modified()
    {
        var originalIssue = new ConnectionConfigurationIssue(
            "password",
            "La contraseña Firebird es obligatoria.");
        var replacementIssue = new ConnectionConfigurationIssue(
            "usuario",
            "El usuario Firebird es obligatorio.");
        var source = new[] { originalIssue };
        var result = new ConnectionTestResult(
            ConnectionTestStatus.InvalidConfiguration,
            "Completa los datos requeridos para conectar con Firebird.",
            TimeSpan.Zero,
            source);

        source[0] = replacementIssue;

        Assert.Same(originalIssue, Assert.Single(result.Issues));
        var exposedList = Assert.IsAssignableFrom<IList<ConnectionConfigurationIssue>>(
            result.Issues);
        Assert.Throws<NotSupportedException>(() => exposedList[0] = replacementIssue);
        Assert.Same(originalIssue, Assert.Single(result.Issues));
    }
}
