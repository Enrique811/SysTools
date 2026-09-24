using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;

namespace SysTools.Business.Licensing;

public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateAsync(
        string? licenseInput,
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
