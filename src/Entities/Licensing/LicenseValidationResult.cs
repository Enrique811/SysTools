namespace SysTools.Entities.Licensing;

public sealed record LicenseValidationResult
{
    public LicenseValidationResult(
        LicenseValidationStatus status,
        LicenseIssuer issuer = LicenseIssuer.None,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        DateTime? serverTime = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (!Enum.IsDefined(issuer))
        {
            throw new ArgumentOutOfRangeException(nameof(issuer));
        }

        if (validFrom is not null && validUntil is not null && validFrom > validUntil)
        {
            throw new ArgumentException("El inicio no puede ser posterior al fin.");
        }

        if (status == LicenseValidationStatus.Valid
            && (issuer == LicenseIssuer.None
                || validFrom is null
                || validUntil is null
                || serverTime is null
                || serverTime < validFrom
                || serverTime > validUntil))
        {
            throw new ArgumentException(
                "Un resultado valido requiere emisor, fechas y tiempo dentro del rango.");
        }

        Status = status;
        Issuer = issuer;
        ValidFrom = NormalizeKind(validFrom);
        ValidUntil = NormalizeKind(validUntil);
        ServerTime = NormalizeKind(serverTime);
    }

    public LicenseValidationStatus Status { get; }

    public string Message => Status switch
    {
        LicenseValidationStatus.Valid => "La licencia es valida.",
        LicenseValidationStatus.MissingInput => "No se configuro la licencia.",
        LicenseValidationStatus.SourceUnavailable => "No fue posible leer el archivo de licencia.",
        LicenseValidationStatus.InvalidJson => "El contenido de la licencia no es JSON valido.",
        LicenseValidationStatus.InvalidFields => "La licencia contiene campos invalidos.",
        LicenseValidationStatus.InvalidSignature => "La firma de la licencia no es valida.",
        LicenseValidationStatus.HardwareIdUnavailable => "No fue posible identificar este equipo.",
        LicenseValidationStatus.HardwareMismatch => "La licencia no corresponde a este equipo.",
        LicenseValidationStatus.NotYetValid => "La licencia aun no ha iniciado.",
        LicenseValidationStatus.Expired => "La licencia esta vencida.",
        LicenseValidationStatus.ServerTimeUnavailable => "No fue posible comprobar la vigencia con el servidor.",
        _ => throw new InvalidOperationException("Estado de licencia no soportado.")
    };

    public LicenseIssuer Issuer { get; }

    public DateTime? ValidFrom { get; }

    public DateTime? ValidUntil { get; }

    public DateTime? ServerTime { get; }

    public bool IsValid => Status == LicenseValidationStatus.Valid;

    private static DateTime? NormalizeKind(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
}
