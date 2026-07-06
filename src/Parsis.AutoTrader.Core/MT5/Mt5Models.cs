namespace Parsis.AutoTrader.Core.MT5;

public sealed record Mt5Terminal(string TerminalPath, string DataPath, string Name);
public sealed record Mt5LaunchRequest(long Login, string Password, string Server, string Symbol, int Magic, int Slippage);
public sealed record Mt5Status(bool Connected, long Login, string Server, decimal Balance, decimal Equity, decimal Margin, decimal FreeMargin, int OpenPositions, string Message, DateTimeOffset Timestamp);
