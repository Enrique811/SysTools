using SysTools.Entities.Licensing;

namespace SysTools.Business.Licensing;

public interface ILicenseSignatureVerifier
{
    LicenseIssuer Verify(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature);
}
