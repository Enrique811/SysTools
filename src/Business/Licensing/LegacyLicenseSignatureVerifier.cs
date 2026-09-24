using System.Security.Cryptography;
using SysTools.Entities.Licensing;

namespace SysTools.Business.Licensing;

public sealed class LegacyLicenseSignatureVerifier : ILicenseSignatureVerifier
{
    private const string DistributorPublicKey =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzl0ie2IjH6sOfJXZquujuncM"
        + "aXfcLbzUDyWNN4dwvZO4PwTsEUl+PYEuT9Iv2BWNHXCZviyfDQjWIZcy4rqfBecFUOSF"
        + "iDfUhaCLk61y/Gx3uqfCGUHnxrCGYNBu5uWwGXLEt1nyOZAfBv4Z1JCCZ03YebZeZkwL"
        + "kYbL517dpKgbxuMHPtdl8p9GmXT8ljUZhao5WojmafxHQBz3jz+59LAUKUhdZb3hwiC+"
        + "526lU0JiBm48yB28dtL6dh3ixm9TOetN0Cwm+nDS0jMtqX+qvhYv/E+Ztz1n5Q++v9Zf"
        + "OTQMmlKI2TVZ1R+pL8kQOvuw/GsdZONcXscfMF7ENVRkAQIDAQAB";

    private const string DeveloperPublicKey =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAlN7pBkLh3rZiY8yPZ/xtcHAaD5eS93SZOMzdTnFAsmxDZ6Y8VIOXopY44Wg6h/CUjVRCFx38ZpqJ8DYqYPi+AdOzEyRwHSjWsPcDJ+xr+1Zt9y3t3eFeSatHpx9C3eIy3AwXJuC56oX1EqBNpNltG+lFFZHjCr+h3lp5iazGbfgii8gtdVC2z5pWGI6/agsmDEngY5vGl2N29lRkusG7sJkBdPEd2ReNnIy7oRZtq9w6RinpQxrLeToN8TGe5B5A/TfRyfInRT0ZY7RNlVvXCVT6J7R/9UdMgHK+gIzsReohV3OMZG+fsjfw7Rx3RxZT3mps+lbcFwvBs6xQteRHWQIDAQAB";

    public LicenseIssuer Verify(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature)
    {
        if (VerifyWithKey(DistributorPublicKey, payload, signature))
        {
            return LicenseIssuer.DistributorColombia;
        }

        return VerifyWithKey(DeveloperPublicKey, payload, signature)
            ? LicenseIssuer.Developer
            : LicenseIssuer.None;
    }

    private static bool VerifyWithKey(
        string encodedKey,
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(encodedKey), out _);
            return rsa.VerifyData(
                payload,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
