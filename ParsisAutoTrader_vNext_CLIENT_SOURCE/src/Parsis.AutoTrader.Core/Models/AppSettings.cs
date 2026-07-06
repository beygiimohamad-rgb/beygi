namespace Parsis.AutoTrader.Core.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "fa-IR";
    public string Theme { get; set; } = "Dark";
    public TelegramOptions Telegram { get; set; } = new();
    public Mt5Options Mt5 { get; set; } = new();
    public RiskOptions Risk { get; set; } = new();
    public bool WindowsNotifications { get; set; } = true;
    public bool DuplicateFilter { get; set; } = true;
}

public sealed class TelegramOptions
{
    public int ApiId { get; set; }
    public string ApiHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<long> ChannelIds { get; set; } = new();
}

public sealed class Mt5Options
{
    public string TerminalPath { get; set; } = string.Empty;
    public long Login { get; set; }
    public string ProtectedPassword { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Symbol { get; set; } = "AUTO";
    public int MagicNumber { get; set; } = 20260706;
    public int SlippagePoints { get; set; } = 20;
}

public sealed class RiskOptions
{
    public bool UseRiskPercent { get; set; }
    public decimal FixedLot { get; set; } = 0.10m;
    public decimal RiskPercent { get; set; } = 1.00m;
    public int MaxConcurrentTrades { get; set; } = 3;
    public decimal Tp1ClosePercent { get; set; } = 50m;
    public decimal Tp2CloseRemainingPercent { get; set; } = 50m;
    public bool MoveStopToBreakEvenAtTp1 { get; set; } = true;
}
