using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class GogDetector
{
    public static Dictionary<string, string> Detect()
    {
        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // GOG Galaxy 2.x — each installed game has a subkey with an "path" value
        using RegistryKey? gamesKey = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\WOW6432Node\GOG.com\Games");

        if (gamesKey is not null)
        {
            foreach (string gameId in gamesKey.GetSubKeyNames())
            {
                using RegistryKey? gameKey = gamesKey.OpenSubKey(gameId);
                string? path = gameKey?.GetValue("path") as string;
                if (path is null || !Directory.Exists(path))
                    continue;

                string? parent = Directory.GetParent(path)?.FullName;
                if (parent is not null && Directory.Exists(parent))
                    installRoots.Add(parent);
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in installRoots)
            result[root] = "GOG Galaxy";

        return result;
    }
}
