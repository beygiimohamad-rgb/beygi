using System.Text.Json;

namespace Parsis.AutoTrader.App;

public sealed record BuildConfig(int TelegramApiId, string TelegramApiHash, string LicensePurchaseUrl)
{
    public static BuildConfig Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var idText = root.GetProperty("TelegramApiId").GetString() ?? "0";
            _ = int.TryParse(idText, out var id);
            return new(id, root.GetProperty("TelegramApiHash").GetString() ?? string.Empty, root.GetProperty("LicensePurchaseUrl").GetString() ?? "https://t.me/parsislic");
        }
        catch { return new(0, string.Empty, "https://t.me/parsislic"); }
    }
}
