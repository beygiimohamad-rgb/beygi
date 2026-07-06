namespace Parsis.AutoTrader.Core.Telegram;
public sealed record TelegramChannel(long Id, string Title, string Username);
public sealed record TelegramMessage(long ChannelId, string ChannelTitle, long MessageId, string Text, DateTimeOffset Timestamp);
