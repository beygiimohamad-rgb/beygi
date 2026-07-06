using Parsis.AutoTrader.Core.Licensing;
using Parsis.AutoTrader.Core.Models;
using Parsis.AutoTrader.Core.MT5;
using Parsis.AutoTrader.Core.Parsing;
using Parsis.AutoTrader.Core.Services;
using Parsis.AutoTrader.Core.Telegram;

namespace Parsis.AutoTrader.App;

public sealed class AppState
{
    public static AppState Current { get; } = new();
    public SettingsService SettingsService { get; } = new();
    public SignalParser Parser { get; } = new();
    public DuplicateSignalFilter DuplicateFilter { get; } = new();
    public Mt5TerminalDiscovery Discovery { get; } = new();
    public Mt5AutomationService Mt5 { get; } = new();
    public TelegramClientService Telegram { get; } = new();
    public LicenseService License { get; } = new();
    public AppSettings Settings { get; private set; } = new();
    public bool Running { get; set; }
    public event EventHandler<string>? LogAdded;
    private AppState()
    {
        Settings = SettingsService.LoadAsync().GetAwaiter().GetResult();
        var build = BuildConfig.Load();
        if (Settings.Telegram.ApiId <= 0) Settings.Telegram.ApiId = build.TelegramApiId;
        if (string.IsNullOrWhiteSpace(Settings.Telegram.ApiHash)) Settings.Telegram.ApiHash = build.TelegramApiHash;
        Telegram.SetSelectedChannels(Settings.Telegram.ChannelIds);
        Telegram.MessageReceived += async (_, msg) =>
        {
            AddLog($"Telegram: {msg.ChannelTitle} → {msg.Text.Replace(Environment.NewLine, " ")}");
            if (!Running || (Settings.DuplicateFilter && DuplicateFilter.IsDuplicate(msg.Text))) return;
            var parsed = Parser.Parse(msg.Text, msg.Timestamp);
            if (!parsed.Success) { AddLog("Signal rejected: " + string.Join("; ", parsed.Errors)); return; }
            var lot = Settings.Risk.FixedLot;
            await Mt5.SendSignalAsync(parsed.Signal!, lot, Settings.Mt5.MagicNumber, Settings.Mt5.SlippagePoints);
            AddLog($"Signal sent to MT5: {parsed.Signal!.Side} {parsed.Signal.Symbol}");
        };
        Mt5.StatusChanged += (_, s) => AddLog($"MT5 {s.Login}: Balance={s.Balance} Equity={s.Equity}");
    }
    public void AddLog(string message) => LogAdded?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
}
