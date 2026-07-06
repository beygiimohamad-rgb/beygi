using Microsoft.Win32;

namespace Parsis.AutoTrader.Core.MT5;

public sealed class Mt5TerminalDiscovery
{
    public IReadOnlyList<Mt5Terminal> Discover()
    {
        var exePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var basePath in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
        {
            if (!Directory.Exists(basePath)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(basePath, "terminal64.exe", SearchOption.AllDirectories).Take(40)) exePaths.Add(f);
            }
            catch { }
        }
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\terminal64.exe");
                if (key?.GetValue(null) is string p && File.Exists(p)) exePaths.Add(p);
            }
            catch { }
        }

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MetaQuotes", "Terminal");
        var terminals = new List<Mt5Terminal>();
        foreach (var exe in exePaths)
        {
            var dir = Directory.GetParent(exe)?.FullName ?? string.Empty;
            var data = FindDataPath(appData, dir);
            if (!string.IsNullOrEmpty(data)) terminals.Add(new(exe, data, Directory.GetParent(exe)?.Name ?? "MetaTrader 5"));
        }
        return terminals.DistinctBy(x => x.TerminalPath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string FindDataPath(string root, string installDir)
    {
        if (!Directory.Exists(root)) return string.Empty;
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            var origin = Path.Combine(folder, "origin.txt");
            try
            {
                if (File.Exists(origin) && Path.GetFullPath(File.ReadAllText(origin).Trim()).TrimEnd('\\')
                    .Equals(Path.GetFullPath(installDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return folder;
            }
            catch { }
        }
        return string.Empty;
    }
}
