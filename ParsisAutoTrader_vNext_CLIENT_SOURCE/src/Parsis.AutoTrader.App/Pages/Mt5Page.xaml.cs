using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Parsis.AutoTrader.Core.MT5;
using Parsis.AutoTrader.Core.Security;

namespace Parsis.AutoTrader.App.Pages;
public sealed partial class Mt5Page : Page
{
    private readonly AppState S = AppState.Current;
    public Mt5Page()
    {
        InitializeComponent(); Load(); S.Mt5.StatusChanged += (_, s) => DispatcherQueue.TryEnqueue(() => StatusText.Text = s.Connected ? $"Connected • {s.Login} • {s.Server}" : s.Message);
    }
    private void Load()
    {
        TerminalCombo.ItemsSource = S.Discovery.Discover(); LoginBox.Value = S.Settings.Mt5.Login; ServerBox.Text = S.Settings.Mt5.Server; SymbolBox.Text = S.Settings.Mt5.Symbol; MagicBox.Value = S.Settings.Mt5.MagicNumber; SlippageBox.Value = S.Settings.Mt5.SlippagePoints;
        if (!string.IsNullOrWhiteSpace(S.Settings.Mt5.TerminalPath)) TerminalCombo.SelectedItem = ((IEnumerable<Mt5Terminal>)TerminalCombo.ItemsSource).FirstOrDefault(x => x.TerminalPath == S.Settings.Mt5.TerminalPath);
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => Load();
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TerminalCombo.SelectedItem is Mt5Terminal t) S.Settings.Mt5.TerminalPath = t.TerminalPath;
        S.Settings.Mt5.Login = (long)LoginBox.Value; S.Settings.Mt5.Server = ServerBox.Text.Trim(); S.Settings.Mt5.Symbol = string.IsNullOrWhiteSpace(SymbolBox.Text) ? "AUTO" : SymbolBox.Text.Trim(); S.Settings.Mt5.MagicNumber = (int)MagicBox.Value; S.Settings.Mt5.SlippagePoints = (int)SlippageBox.Value;
        if (!string.IsNullOrWhiteSpace(PasswordBox.Password)) S.Settings.Mt5.ProtectedPassword = SecretProtector.Protect(PasswordBox.Password);
        await S.SettingsService.SaveAsync(S.Settings); StatusText.Text = "Saved";
    }
    private async void Connect_Click(object sender, RoutedEventArgs e) { Save_Click(sender, e); await Task.Delay(150); var t = TerminalCombo.SelectedItem as Mt5Terminal ?? S.Discovery.Discover().FirstOrDefault(); if (t is null) { StatusText.Text = "No terminal found"; return; } try { StatusText.Text = "Starting…"; await S.Mt5.StartAsync(t, S.Settings.Mt5); } catch (Exception ex) { StatusText.Text = ex.Message; } }
}
