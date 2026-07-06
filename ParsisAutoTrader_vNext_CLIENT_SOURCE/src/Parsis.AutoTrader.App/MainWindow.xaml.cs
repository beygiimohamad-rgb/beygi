using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Parsis.AutoTrader.App.Pages;

namespace Parsis.AutoTrader.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }
    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected) { ContentFrame.Navigate(typeof(SettingsPage)); return; }
        if (args.SelectedItemContainer?.Tag is not string tag) return;
        ContentFrame.Navigate(tag switch
        {
            "telegram" => typeof(TelegramPage), "mt5" => typeof(Mt5Page), "risk" => typeof(RiskPage), "logs" => typeof(LogsPage), _ => typeof(DashboardPage)
        });
    }
}
