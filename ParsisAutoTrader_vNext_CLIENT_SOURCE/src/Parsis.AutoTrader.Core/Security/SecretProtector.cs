using System.Security.Cryptography;
using System.Text;

namespace Parsis.AutoTrader.Core.Security;

public static class SecretProtector
{
    private static readonly byte[] Entropy = SHA256.HashData(Encoding.UTF8.GetBytes("Parsis.AutoTrader.vNext"));
    public static string Protect(string plain) => string.IsNullOrEmpty(plain) ? string.Empty :
        Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser));
    public static string Unprotect(string protectedValue) => string.IsNullOrEmpty(protectedValue) ? string.Empty :
        Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedValue), Entropy, DataProtectionScope.CurrentUser));
}
