namespace Parsis.AutoTrader.Core.Models;

public enum TradeSide { Buy, Sell }
public enum EntryMode { Market, Limit, Stop }

public sealed record TradingSignal(
    string Symbol,
    TradeSide Side,
    EntryMode EntryMode,
    decimal EntryLow,
    decimal EntryHigh,
    decimal StopLoss,
    IReadOnlyList<decimal> Targets,
    string RawText,
    DateTimeOffset ReceivedAt,
    double Confidence)
{
    public decimal PreferredEntry => EntryLow == 0 ? EntryHigh : EntryHigh == 0 ? EntryLow : (EntryLow + EntryHigh) / 2m;
}

public sealed record SignalParseResult(bool Success, TradingSignal? Signal, IReadOnlyList<string> Errors)
{
    public static SignalParseResult Fail(params string[] errors) => new(false, null, errors);
    public static SignalParseResult Ok(TradingSignal signal) => new(true, signal, Array.Empty<string>());
}
