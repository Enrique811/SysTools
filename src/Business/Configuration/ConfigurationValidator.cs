using SysTools.Entities.Configuration;

namespace SysTools.Business.Configuration;

public sealed class ConfigurationValidator
{
    public ConfigurationValidationResult Validate(AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var issues = new List<ConfigurationIssue>();
        AddRequiredIssueIfBlank(issues, "ipEmpresa", configuration.IpEmpresa);
        AddRequiredIssueIfBlank(issues, "rutaEmpresa", configuration.RutaEmpresa);
        AddRequiredIssueIfBlank(issues, "usuario", configuration.Usuario);
        AddRequiredIssueIfBlank(issues, "password", configuration.Password);

        if (configuration.Ambiente is not ("a" or "b"))
        {
            issues.Add(new(
                ConfigurationIssueCode.UnsupportedEnvironment,
                "ambiente",
                "El ambiente debe ser 'a' o 'b'.",
                ConfigurationIssueSeverity.Error));
        }

        if (configuration.FormatoPrecio is not ("CO" or "MX"))
        {
            issues.Add(new(
                ConfigurationIssueCode.UnsupportedPriceFormat,
                "formatoPrecio",
                "El formato de precio debe ser 'CO' o 'MX'.",
                ConfigurationIssueSeverity.Error));
        }

        if (configuration.Columnas is < 1 or > 3)
        {
            issues.Add(new(
                ConfigurationIssueCode.UnsupportedColumnCount,
                "columnas",
                "La cantidad de columnas debe ser 1, 2 o 3.",
                ConfigurationIssueSeverity.Error));
        }

        var isPersistable = issues.All(issue => issue.Severity != ConfigurationIssueSeverity.Error);
        var isConnectionReady = isPersistable &&
            issues.All(issue => issue.Code != ConfigurationIssueCode.RequiredForConnection);

        return new ConfigurationValidationResult(isPersistable, isConnectionReady, issues);
    }

    private static void AddRequiredIssueIfBlank(
        ICollection<ConfigurationIssue> issues,
        string field,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) return;

        issues.Add(new(
            ConfigurationIssueCode.RequiredForConnection,
            field,
            $"El campo {field} es necesario para conectar.",
            ConfigurationIssueSeverity.Warning));
    }
}
