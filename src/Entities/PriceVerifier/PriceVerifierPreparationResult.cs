using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;

namespace SysTools.Entities.PriceVerifier;

public sealed class PriceVerifierPreparationResult
{
    public PriceVerifierPreparationResult(
        PriceVerifierPreparationStatus status,
        string message,
        ConnectionTestStatus? connectionStatus = null,
        LicenseValidationStatus? licenseStatus = null,
        string? additionalInformation = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));
        }

        Validate(status, connectionStatus, licenseStatus);
        Status = status;
        Message = message;
        ConnectionStatus = connectionStatus;
        LicenseStatus = licenseStatus;
        AdditionalInformation = additionalInformation ?? string.Empty;
    }

    public PriceVerifierPreparationStatus Status { get; }
    public string Message { get; }
    public ConnectionTestStatus? ConnectionStatus { get; }
    public LicenseValidationStatus? LicenseStatus { get; }
    public string AdditionalInformation { get; }
    public bool IsReady => Status == PriceVerifierPreparationStatus.Ready;

    private static void Validate(
        PriceVerifierPreparationStatus status,
        ConnectionTestStatus? connectionStatus,
        LicenseValidationStatus? licenseStatus)
    {
        var valid = status switch
        {
            PriceVerifierPreparationStatus.Ready =>
                connectionStatus == ConnectionTestStatus.Success
                && licenseStatus == LicenseValidationStatus.Valid,
            PriceVerifierPreparationStatus.ConfigurationUnavailable =>
                connectionStatus is null && licenseStatus is null,
            PriceVerifierPreparationStatus.ConnectionUnavailable =>
                connectionStatus is not null
                && connectionStatus != ConnectionTestStatus.Success
                && licenseStatus is null,
            PriceVerifierPreparationStatus.LicenseUnavailable =>
                connectionStatus == ConnectionTestStatus.Success
                && licenseStatus is not null
                && licenseStatus != LicenseValidationStatus.Valid,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException("Los estados de preparacion no son consistentes.");
        }
    }
}

