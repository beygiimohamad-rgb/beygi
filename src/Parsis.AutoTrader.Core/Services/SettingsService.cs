using System.Text.Json;
using Parsis.AutoTrader.Core.Models;

namespace Parsis.AutoTrader.Core.Services;

public sealed class SettingsService
{
    private readonly string _path;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public SettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParsisAutoTrader");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }
    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path)) return new AppSettings();
        await using var s = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(s, _json) ?? new AppSettings();
    }
    public async Task SaveAsync(AppSettings settings)
    {
        await using var s = File.Create(_path);
        await JsonSerializer.SerializeAsync(s, settings, _json);
    }
}
