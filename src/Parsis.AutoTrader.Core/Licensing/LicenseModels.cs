namespace Parsis.AutoTrader.Core.Licensing;
public enum LicenseKind { Trial, Standard }
public sealed record LicensePayload(string Customer, string DeviceId, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, LicenseKind Kind, string LicenseId);
public sealed record LicenseStatus(bool IsValid, bool IsTrial, DateTimeOffset ExpiresAt, TimeSpan Remaining, string Message, string DeviceId);
internal sealed record TrialState(DateTimeOffset FirstRunUtc, DateTimeOffset LastSeenUtc, string DeviceId);
