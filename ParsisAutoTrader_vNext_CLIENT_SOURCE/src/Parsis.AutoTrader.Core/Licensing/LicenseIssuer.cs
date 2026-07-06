using System.Security.Cryptography;
using System.Text.Json;

namespace Parsis.AutoTrader.Core.Licensing;

public sealed class LicenseIssuer
{
    public static string Issue(LicensePayload payload, string privatePem)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var rsa = RSA.Create(); rsa.ImportFromPem(privatePem);
        var sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Base64Url(data) + "." + Base64Url(sig);
    }
    private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+','-').Replace('/','_');
}
