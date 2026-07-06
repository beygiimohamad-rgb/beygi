using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Parsis.AutoTrader.Core.Models;
using Parsis.AutoTrader.Core.Security;

namespace Parsis.AutoTrader.Core.MT5;

public sealed class Mt5AutomationService
{
    private readonly string _runtimeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParsisAutoTrader", "Runtime");
    private CancellationTokenSource? _monitorCts;
    public event EventHandler<Mt5Status>? StatusChanged;

    public async Task StartAsync(Mt5Terminal terminal, Mt5Options options, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_runtimeDir);
        await InstallAndCompileBridgeAsync(terminal, ct);
        var symbol = string.IsNullOrWhiteSpace(options.Symbol) || options.Symbol.Equals("AUTO", StringComparison.OrdinalIgnoreCase) ? "XAUUSD" : options.Symbol;
        var ini = Path.Combine(_runtimeDir, $"mt5-{Guid.NewGuid():N}.ini");
        var password = SecretProtector.Unprotect(options.ProtectedPassword);
        var text = $"""
[Common]
Login={options.Login}
Server={options.Server}
Password={password}
KeepPrivate=0
NewsEnable=0

[Experts]
Enabled=1
AllowLiveTrading=1
AllowDllImport=0
Account=0
Profile=0

[StartUp]
Expert=Parsis\\ParsisXauBridge
Symbol={symbol}
Period=M1
ExpertParameters=ParsisXauBridge.set
""";
        await File.WriteAllTextAsync(ini, text, new UTF8Encoding(false), ct);
        var psi = new ProcessStartInfo(terminal.TerminalPath, $"/config:\"{ini}\"") { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(terminal.TerminalPath)! };
        Process.Start(psi);
        _ = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(25)); try { File.Delete(ini); } catch { } });
        _monitorCts?.Cancel();
        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = MonitorStatusAsync(_monitorCts.Token);
    }

    public void StopMonitoring() { _monitorCts?.Cancel(); _monitorCts = null; }

    public async Task SendSignalAsync(TradingSignal signal, decimal volume, int magic, int slippage, CancellationToken ct = default)
    {
        var common = CommonBridgeDirectory();
        Directory.CreateDirectory(Path.Combine(common, "commands"));
        var id = Guid.NewGuid().ToString("N");
        var targets = signal.Targets.Concat(Enumerable.Repeat(signal.Targets.Last(), 3)).Take(3).ToArray();
        var line = string.Join('|', id, "OPEN", signal.Side.ToString().ToUpperInvariant(), signal.EntryMode.ToString().ToUpperInvariant(), signal.Symbol,
            volume.ToString(System.Globalization.CultureInfo.InvariantCulture), signal.EntryLow, signal.EntryHigh, signal.StopLoss,
            targets[0], targets[1], targets[2], magic, slippage);
        var temp = Path.Combine(common, "commands", id + ".tmp");
        var target = Path.Combine(common, "commands", id + ".cmd");
        await File.WriteAllTextAsync(temp, line, ct);
        File.Move(temp, target, true);
    }

    public async Task EmergencyAsync(string action, int magic, CancellationToken ct = default)
    {
        var common = CommonBridgeDirectory(); Directory.CreateDirectory(Path.Combine(common, "commands"));
        var id = Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(Path.Combine(common, "commands", id + ".cmd"), $"{id}|{action}|{magic}", ct);
    }

    private async Task InstallAndCompileBridgeAsync(Mt5Terminal terminal, CancellationToken ct)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "MT5", "ParsisXauBridge.mq5");
        if (!File.Exists(source)) throw new FileNotFoundException("Embedded MT5 bridge source not found.", source);
        var experts = Path.Combine(terminal.DataPath, "MQL5", "Experts", "Parsis");
        var presets = Path.Combine(terminal.DataPath, "MQL5", "Presets");
        Directory.CreateDirectory(experts); Directory.CreateDirectory(presets);
        var mq5 = Path.Combine(experts, "ParsisXauBridge.mq5"); File.Copy(source, mq5, true);
        await File.WriteAllTextAsync(Path.Combine(presets, "ParsisXauBridge.set"), "PollMilliseconds=250\r\n", ct);
        var editor = Path.Combine(Path.GetDirectoryName(terminal.TerminalPath)!, "metaeditor64.exe");
        if (!File.Exists(editor)) throw new FileNotFoundException("MetaEditor was not found.", editor);
        var log = Path.Combine(_runtimeDir, "metaeditor-compile.log");
        var p = Process.Start(new ProcessStartInfo(editor, $"/compile:\"{mq5}\" /log:\"{log}\"") { UseShellExecute = false, CreateNoWindow = true });
        if (p is null) throw new InvalidOperationException("Could not start MetaEditor.");
        await p.WaitForExitAsync(ct);
        var ex5 = Path.ChangeExtension(mq5, ".ex5");
        if (!File.Exists(ex5)) throw new InvalidOperationException("MT5 bridge compilation failed. See: " + log);
    }

    private async Task MonitorStatusAsync(CancellationToken ct)
    {
        var statusFile = Path.Combine(CommonBridgeDirectory(), "status.json");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(statusFile))
                {
                    var json = await File.ReadAllTextAsync(statusFile, ct);
                    var status = JsonSerializer.Deserialize<Mt5Status>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (status is not null) StatusChanged?.Invoke(this, status);
                }
            }
            catch { }
            await Task.Delay(1000, ct);
        }
    }

    public static string CommonBridgeDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MetaQuotes", "Terminal", "Common", "Files", "ParsisAutoTrader");
}
