using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Parsis.AutoTrader.Core.Security;

namespace Parsis.AutoTrader.Core.Licensing;

public sealed class LicenseService
{
    private readonly string _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParsisAutoTrader", "License");
    private readonly string _publicKeyPath = Path.Combine(AppContext.BaseDirectory, "Assets", "license_public.pem");
    public string DeviceId => GetDeviceId();

    public LicenseStatus GetStatus()
    {
        Directory.CreateDirectory(_dir);
        var licensed = ValidateLicenseFile();
        if (licensed is not null) return licensed;
        return GetTrialStatus();
    }

    public LicenseStatus InstallLicense(string token)
    {
        var parsed = ValidateToken(token);
        if (parsed is null) return new(false, false, DateTimeOffset.MinValue, TimeSpan.Zero, "Invalid license signature.", DeviceId);
        if (!string.Equals(parsed.DeviceId, DeviceId, StringComparison.OrdinalIgnoreCase) && parsed.DeviceId != "*")
            return new(false, false, parsed.ExpiresAt, TimeSpan.Zero, "License belongs to another device.", DeviceId);
        File.WriteAllText(Path.Combine(_dir, "license.key"), token.Trim());
        return GetStatus();
    }

    private LicenseStatus? ValidateLicenseFile()
    {
        var path = Path.Combine(_dir, "license.key");
        if (!File.Exists(path)) return null;
        var payload = ValidateToken(File.ReadAllText(path));
        if (payload is null) return null;
        var now = DateTimeOffset.UtcNow;
        var validDevice = payload.DeviceId == "*" || payload.DeviceId.Equals(DeviceId, StringComparison.OrdinalIgnoreCase);
        var valid = validDevice && payload.ExpiresAt > now;
        return new(valid, false, payload.ExpiresAt, payload.ExpiresAt - now, valid ? "License active" : "License expired or device mismatch", DeviceId);
    }

    private LicensePayload? ValidateToken(string token)
    {
        try
        {
            var parts = token.Trim().Split('.'); if (parts.Length != 2) return null;
            var data = Base64UrlDecode(parts[0]); var sig = Base64UrlDecode(parts[1]);
            using var rsa = RSA.Create(); rsa.ImportFromPem(File.ReadAllText(_publicKeyPath));
            if (!rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)) return null;
            return JsonSerializer.Deserialize<LicensePayload>(data);
        }
        catch { return null; }
    }

    private LicenseStatus GetTrialStatus()
    {
        var now = DateTimeOffset.UtcNow; var path = Path.Combine(_dir, "trial.dat"); TrialState state;
        try
        {
            state = File.Exists(path) ? JsonSerializer.Deserialize<TrialState>(SecretProtector.Unprotect(File.ReadAllText(path)))! : new(now, now, DeviceId);
            if (state is null || state.DeviceId != DeviceId) state = new(now, now, DeviceId);
        }
        catch { state = new(now, now, DeviceId); }
        var rollback = now < state.LastSeenUtc.AddMinutes(-10);
        var expires = state.FirstRunUtc.AddDays(3);
        state = state with { LastSeenUtc = now > state.LastSeenUtc ? now : state.LastSeenUtc };
        File.WriteAllText(path, SecretProtector.Protect(JsonSerializer.Serialize(state)));
        var valid = !rollback && now < expires;
        return new(valid, true, expires, expires - now, rollback ? "System clock rollback detected." : valid ? "3-day trial active" : "Trial expired", DeviceId);
    }

    private static string GetDeviceId()
    {
        string machine = "unknown";
        try { machine = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")?.GetValue("MachineGuid")?.ToString() ?? Environment.MachineName; } catch { }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(machine + "|ParsisAutoTrader")))[..24];
    }
    private static byte[] Base64UrlDecode(string s) => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + new string('=', (4 - s.Length % 4) % 4));
}
