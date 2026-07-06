using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Parsis.AutoTrader.Core.Security;

namespace Parsis.AutoTrader.App.Pages;
public sealed partial class DashboardPage : Page
{
    private readonly AppState S = AppState.Current;
    public DashboardPage()
    {
        InitializeComponent();
        var lic = S.License.GetStatus();
        LicenseStatus.Text = lic.IsValid ? $"{(lic.IsTrial ? "Trial" : "Active")} • {lic.Remaining.Days}d" : lic.Message;
        S.LogAdded += OnLog;
    }
    private void OnLog(object? sender, string e) => DispatcherQueue.TryEnqueue(() => LatestLog.Text = e);
    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        var lic = S.License.GetStatus(); if (!lic.IsValid) { await Dialog("License", lic.Message); return; }
        var terminals = S.Discovery.Discover();
        var selected = terminals.FirstOrDefault(t => string.IsNullOrWhiteSpace(S.Settings.Mt5.TerminalPath) || t.TerminalPath.Equals(S.Settings.Mt5.TerminalPath, StringComparison.OrdinalIgnoreCase));
        if (selected is null) { await Dialog("MetaTrader 5", "No MT5 terminal was detected. Install MT5 or select terminal in settings."); return; }
        if (S.Settings.Mt5.Login <= 0 || string.IsNullOrWhiteSpace(S.Settings.Mt5.ProtectedPassword) || string.IsNullOrWhiteSpace(S.Settings.Mt5.Server)) { await Dialog("MT5 settings", "Enter login, password and server first."); return; }
        try { RuntimeState.Text = "Starting…"; await S.Mt5.StartAsync(selected, S.Settings.Mt5); S.Running = true; RuntimeState.Text = "Running"; Mt5Status.Text = "Starting"; Mt5Status.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen); }
        catch (Exception ex) { RuntimeState.Text = "Error"; await Dialog("Start failed", ex.Message); }
    }
    private void Stop_Click(object sender, RoutedEventArgs e) { S.Running = false; RuntimeState.Text = "Managing open trades"; S.AddLog("New signals stopped; open trades continue under EA management."); }
    private async void Emergency_Click(object sender, RoutedEventArgs e) { S.Running = false; await S.Mt5.EmergencyAsync("CLOSE_MAGIC", S.Settings.Mt5.MagicNumber); RuntimeState.Text = "Emergency stop"; }
    private async Task Dialog(string title, string content) { var d = new ContentDialog { Title = title, Content = content, CloseButtonText = "OK", XamlRoot = XamlRoot }; await d.ShowAsync(); }
}
