using System.Security.Cryptography;
using System.Text;

namespace Parsis.AutoTrader.Core.Services;

public sealed class DuplicateSignalFilter
{
    private readonly Dictionary<string, DateTimeOffset> _seen = new();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(12);
    public bool IsDuplicate(string text)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var k in _seen.Where(x => now - x.Value > _ttl).Select(x => x.Key).ToList()) _seen.Remove(k);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim())));
        if (_seen.ContainsKey(hash)) return true;
        _seen[hash] = now;
        return false;
    }
}
