using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Parsis.AutoTrader.Core.Telegram;

namespace Parsis.AutoTrader.App.Pages;
public sealed partial class TelegramPage : Page
{
    private readonly AppState S = AppState.Current; private string? _needed;
    public TelegramPage() { InitializeComponent(); PhoneBox.Text = S.Settings.Telegram.PhoneNumber; }
    private (int id,string hash) Api() => (S.Settings.Telegram.ApiId, S.Settings.Telegram.ApiHash);
    private async void PhoneLogin_Click(object sender, RoutedEventArgs e)
    {
        var api=Api(); if(api.id<=0||string.IsNullOrWhiteSpace(api.hash)){LoginStatus.Text="Telegram API is not embedded in this build.";return;}
        var input = _needed is null ? PhoneBox.Text.Trim() : CodeBox.Text.Trim(); if (_needed is null) S.Settings.Telegram.PhoneNumber = PhoneBox.Text.Trim();
        try { _needed = await S.Telegram.LoginWithPhoneAsync(api.id, api.hash, SessionPath(), input); LoginStatus.Text = _needed is null ? $"Connected: {S.Telegram.CurrentUser}" : $"Enter: {_needed}"; if (_needed is null) { await S.SettingsService.SaveAsync(S.Settings); await LoadChannels(); } }
        catch(Exception ex){LoginStatus.Text=ex.Message;}
    }
    private async void QrLogin_Click(object sender, RoutedEventArgs e)
    {
        var api=Api(); if(api.id<=0||string.IsNullOrWhiteSpace(api.hash)){LoginStatus.Text="Telegram API is not embedded in this build.";return;}
        try { await S.Telegram.LoginWithQrAsync(api.id, api.hash, SessionPath(), bytes => DispatcherQueue.TryEnqueue(async () => { using var stream = new InMemoryRandomAccessStream(); await stream.WriteAsync(bytes.AsBuffer()); stream.Seek(0); var bmp=new BitmapImage(); await bmp.SetSourceAsync(stream); QrImage.Source=bmp; })); LoginStatus.Text="Connected"; await LoadChannels(); } catch(Exception ex){LoginStatus.Text=ex.Message;}
    }
    private async void LoadChannels_Click(object sender, RoutedEventArgs e)=>await LoadChannels();
    private async Task LoadChannels(){ChannelList.ItemsSource=await S.Telegram.GetChannelsAsync();}
    private async void SaveChannels_Click(object sender, RoutedEventArgs e){var ids=ChannelList.SelectedItems.Cast<TelegramChannel>().Select(x=>x.Id).ToList();S.Settings.Telegram.ChannelIds=ids;S.Telegram.SetSelectedChannels(ids);await S.SettingsService.SaveAsync(S.Settings);LoginStatus.Text=$"{ids.Count} channel(s) selected";}
    private static string SessionPath(){var d=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ParsisAutoTrader","Telegram");Directory.CreateDirectory(d);return Path.Combine(d,"telegram.session");}
}
