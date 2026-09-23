using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace SysTools.Data.Configuration;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[]? protectedBytes = null;
        try
        {
            protectedBytes = ProtectedData.Protect(
                plaintextBytes,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            if (protectedBytes is not null) CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        var protectedBytes = Convert.FromBase64String(protectedValue);
        byte[]? plaintextBytes = null;
        try
        {
            plaintextBytes = ProtectedData.Unprotect(
                protectedBytes,
                optionalEntropy: null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (plaintextBytes is not null) CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }
}
