using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Parsis.AutoTrader.Core.Models;

namespace Parsis.AutoTrader.Core.Parsing;

public sealed partial class SignalParser
{
    private static readonly Dictionary<char, char> Digits = new()
    {
        ['۰']='0',['۱']='1',['۲']='2',['۳']='3',['۴']='4',['۵']='5',['۶']='6',['۷']='7',['۸']='8',['۹']='9',
        ['٠']='0',['١']='1',['٢']='2',['٣']='3',['٤']='4',['٥']='5',['٦']='6',['٧']='7',['٨']='8',['٩']='9'
    };

    public SignalParseResult Parse(string raw, DateTimeOffset? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return SignalParseResult.Fail("Signal is empty.");
        var text = Normalize(raw);
        var errors = new List<string>();

        var side = DetectSide(text);
        if (side is null) errors.Add("BUY/SELL or خرید/فروش was not found.");

        var symbol = SymbolRegex().Match(text).Value.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(symbol)) symbol = "XAUUSD";

        var mode = DetectEntryMode(text, side);
        var entry = ExtractEntry(text, side);
        var sl = ExtractLabelValue(text, @"(?:SL|STOP\s*LOSS|حد\s*ضرر|استاپ)");
        var targets = ExtractTargets(text);

        if (entry.low <= 0 && mode != EntryMode.Market) errors.Add("Entry price was not detected.");
        if (sl <= 0) errors.Add("Stop loss was not detected.");
        if (targets.Count == 0) errors.Add("No take-profit target was detected.");
        if (side is not null && sl > 0 && entry.low > 0)
        {
            var center = (entry.low + entry.high) / 2m;
            if (side == TradeSide.Buy && sl >= center) errors.Add("BUY stop loss must be below entry.");
            if (side == TradeSide.Sell && sl <= center) errors.Add("SELL stop loss must be above entry.");
        }
        if (errors.Count > 0) return new(false, null, errors);

        while (targets.Count < 3) targets.Add(targets[^1]);
        targets = targets.Take(3).ToList();
        var confidence = 0.55 + (symbol != "XAUUSD" || text.Contains("XAUUSD") || text.Contains("GOLD") ? .1 : 0)
                               + (entry.low > 0 ? .1 : 0) + (targets.Count >= 3 ? .15 : 0) + (sl > 0 ? .1 : 0);

        return SignalParseResult.Ok(new TradingSignal(symbol, side!.Value, mode, entry.low, entry.high, sl,
            targets, raw, receivedAt ?? DateTimeOffset.UtcNow, Math.Min(confidence, 0.99)));
    }

    public static string Normalize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input.Normalize(NormalizationForm.FormKC))
            sb.Append(Digits.TryGetValue(ch, out var d) ? d : ch switch { '٫'=>'.', '٬'=>',', '–'=>'-', '—'=>'-', _=>ch });
        return Regex.Replace(sb.ToString(), @"[ \t]+", " ").Replace("：", ":").ToUpperInvariant();
    }

    private static TradeSide? DetectSide(string text)
    {
        if (Regex.IsMatch(text, @"\b(BUY|LONG)\b|خرید")) return TradeSide.Buy;
        if (Regex.IsMatch(text, @"\b(SELL|SHORT)\b|فروش")) return TradeSide.Sell;
        return null;
    }

    private static EntryMode DetectEntryMode(string text, TradeSide? side)
    {
        if (Regex.IsMatch(text, @"\b(BUY|SELL)\s+STOP\b|استاپ\s*(خرید|فروش)")) return EntryMode.Stop;
        if (Regex.IsMatch(text, @"\b(BUY|SELL)\s+LIMIT\b|لیمیت|معلق")) return EntryMode.Limit;
        return EntryMode.Market;
    }

    private static (decimal low, decimal high) ExtractEntry(string text, TradeSide? side)
    {
        var labelled = EntryRegex().Match(text);
        if (labelled.Success) return ParseRange(labelled.Groups[1].Value, labelled.Groups[2].Value);

        var sideMatch = SideEntryRegex().Match(text);
        if (sideMatch.Success) return ParseRange(sideMatch.Groups[1].Value, sideMatch.Groups[2].Value);
        return (0, 0);
    }

    private static decimal ExtractLabelValue(string text, string label)
    {
        var m = Regex.Match(text, label + @"\s*(?:\d|اول|دوم|سوم)?\s*[:=\-]?\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
        return m.Success && decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static List<decimal> ExtractTargets(string text)
    {
        var result = new List<decimal>();
        foreach (Match m in TargetRegex().Matches(text))
            if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 0) result.Add(v);
        return result.Distinct().Take(3).ToList();
    }

    private static (decimal low, decimal high) ParseRange(string a, string b)
    {
        _ = decimal.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var x);
        _ = decimal.TryParse(string.IsNullOrWhiteSpace(b) ? a : b, NumberStyles.Any, CultureInfo.InvariantCulture, out var y);
        return (Math.Min(x, y), Math.Max(x, y));
    }

    [GeneratedRegex(@"\b(?:XAUUSD|GOLD)(?:[._-]?[A-Z0-9]+)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex SymbolRegex();
    [GeneratedRegex(@"(?:ENTRY|ENTER|PRICE|ورود|نقطه\s*ورود)\s*[:=\-]?\s*([0-9]+(?:\.[0-9]+)?)(?:\s*(?:-|TO|تا)\s*([0-9]+(?:\.[0-9]+)?))?", RegexOptions.IgnoreCase)]
    private static partial Regex EntryRegex();
    [GeneratedRegex(@"(?:\bBUY\b|\bSELL\b|خرید|فروش)(?:\s+(?:LIMIT|STOP))?(?:\s+(?:XAUUSD|GOLD)(?:[._-]?[A-Z0-9]+)?)?\s*[:@-]?\s*([0-9]+(?:\.[0-9]+)?)(?:\s*(?:-|TO|تا)\s*([0-9]+(?:\.[0-9]+)?))?", RegexOptions.IgnoreCase)]
    private static partial Regex SideEntryRegex();
    [GeneratedRegex(@"(?:TP\s*[123]?|TARGET\s*(?:[123]|ONE|TWO|THREE)?|هدف\s*(?:اول|دوم|سوم|[123])?)\s*[:=\-]?\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex TargetRegex();
}
