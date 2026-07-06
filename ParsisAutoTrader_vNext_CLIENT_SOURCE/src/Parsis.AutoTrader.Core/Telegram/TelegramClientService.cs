using QRCoder;
using TL;

namespace Parsis.AutoTrader.Core.Telegram;

public sealed class TelegramClientService : IAsyncDisposable
{
    private WTelegram.Client? _client;
    private WTelegram.UpdateManager? _manager;
    private readonly HashSet<long> _selected = new();
    public event EventHandler<TelegramMessage>? MessageReceived;
    public User? CurrentUser => _client?.User;

    public async Task<string?> LoginWithPhoneAsync(int apiId, string apiHash, string sessionPath, string loginInfo, CancellationToken ct = default)
    {
        _client ??= new WTelegram.Client(apiId, apiHash, sessionPath);
        var needed = await _client.Login(loginInfo);
        if (_client.User is not null) await StartUpdatesAsync();
        return needed;
    }

    public async Task<byte[]> LoginWithQrAsync(int apiId, string apiHash, string sessionPath, Action<byte[]> onQr, CancellationToken ct = default)
    {
        _client ??= new WTelegram.Client(apiId, apiHash, sessionPath);
        await _client.LoginWithQRCode(url =>
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(9);
            onQr(png);
        }, ct: ct);
        await StartUpdatesAsync();
        return Array.Empty<byte>();
    }

    public async Task<IReadOnlyList<TelegramChannel>> GetChannelsAsync()
    {
        if (_client?.User is null) return Array.Empty<TelegramChannel>();
        var dialogs = await _client.Messages_GetAllDialogs();
        return dialogs.chats.Values.OfType<Channel>().Where(c => c.IsActive)
            .Select(c => new TelegramChannel(c.ID, c.title, c.username ?? string.Empty)).OrderBy(c => c.Title).ToList();
    }

    public void SetSelectedChannels(IEnumerable<long> ids)
    {
        _selected.Clear(); foreach (var id in ids) _selected.Add(id);
    }

    private async Task StartUpdatesAsync()
    {
        if (_client is null || _manager is not null) return;
        _manager = _client.WithUpdateManager(OnUpdate);
        var dialogs = await _client.Messages_GetAllDialogs();
        dialogs.CollectUsersChats(_manager.Users, _manager.Chats);
    }

    private Task OnUpdate(Update update)
    {
        if (update is UpdateNewMessage { message: Message m } && m.peer_id is PeerChannel pc && _selected.Contains(pc.channel_id) && !string.IsNullOrWhiteSpace(m.message))
        {
            var title = _manager?.Chats.TryGetValue(pc.channel_id, out var chat) == true ? chat.Title : pc.channel_id.ToString();
            MessageReceived?.Invoke(this, new TelegramMessage(pc.channel_id, title, m.ID, m.message, DateTimeOffset.FromUnixTimeSeconds(m.date)));
        }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null) await _client.DisposeAsync();
    }
}
