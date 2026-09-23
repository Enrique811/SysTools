using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Business.Connection;

public sealed class ConnectionConfigurationValidator
{
    public IReadOnlyList<ConnectionConfigurationIssue> Validate(
        AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var issues = new List<ConnectionConfigurationIssue>(capacity: 4);
        AddIssueWhenMissing(
            issues,
            "ipEmpresa",
            configuration.IpEmpresa,
            "El servidor Firebird es obligatorio.");
        AddIssueWhenMissing(
            issues,
            "rutaEmpresa",
            configuration.RutaEmpresa,
            "La ruta de la base de datos es obligatoria.");
        AddIssueWhenMissing(
            issues,
            "usuario",
            configuration.Usuario,
            "El usuario Firebird es obligatorio.");
        AddIssueWhenMissing(
            issues,
            "password",
            configuration.Password,
            "La contraseña Firebird es obligatoria.");
        return issues;
    }

    private static void AddIssueWhenMissing(
        ICollection<ConnectionConfigurationIssue> issues,
        string field,
        string value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new ConnectionConfigurationIssue(field, message));
        }
    }
}
