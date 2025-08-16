using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Client.WinForms.Infrastructure
{
    public static class ProtocolRegistrar
    {
        private const string Scheme = "connect4";

        // Register connect4:// protocol for current user
        public static void EnsureRegistered()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string rootPath = $@"Software\Classes\{Scheme}";

                using var root = Registry.CurrentUser.CreateSubKey(rootPath);
                root!.SetValue(string.Empty, "URL:Connect4 Protocol");
                root.SetValue("URL Protocol", string.Empty);

                using var icon = root.CreateSubKey("DefaultIcon");
                icon!.SetValue(string.Empty, $"\"{exePath}\",1");

                using var cmd = root.CreateSubKey(@"shell\open\command");
                cmd!.SetValue(string.Empty, $"\"{exePath}\" \"%1\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Protocol registration failed: " + ex);
            }
        }

        // Parse connect4://new-game?playerId=123
        public static (bool IsProtocolLaunch, int? PlayerId) TryParseProtocolArgs(string[] args)
        {
            if (args == null || args.Length == 0) return (false, null);

            var first = args[0];
            if (!first.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase))
                return (false, null);

            try
            {
                var uri = new Uri(first);
                if (!uri.Host.Equals("new-game", StringComparison.OrdinalIgnoreCase))
                    return (true, null);

                var query = uri.Query.TrimStart('?')
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split(new[] { '=' }, 2))
                    .ToDictionary(
                        kv => Uri.UnescapeDataString(kv[0]),
                        kv => kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "",
                        StringComparer.OrdinalIgnoreCase);

                if (query.TryGetValue("playerId", out var val) && int.TryParse(val, out var pid))
                    return (true, pid);

                return (true, null);
            }
            catch
            {
                return (true, null);
            }
        }
    }
}
