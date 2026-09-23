namespace SysTools.Entities.Connection;

public sealed record ConnectionConfigurationIssue
{
    private static readonly HashSet<string> AllowedFields =
        new(StringComparer.Ordinal)
        {
            "ipEmpresa",
            "rutaEmpresa",
            "usuario",
            "password"
        };

    public ConnectionConfigurationIssue(string field, string message)
    {
        if (!AllowedFields.Contains(field))
        {
            throw new ArgumentException("El campo de conexión no es válido.", nameof(field));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));
        }

        Field = field;
        Message = message;
    }

    public string Field { get; }

    public string Message { get; }
}
