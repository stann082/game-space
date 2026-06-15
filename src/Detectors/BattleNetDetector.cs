using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class BattleNetDetector
{
    public static Dictionary<string, string> Detect()
    {
        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] keys =
        [
            @"SOFTWARE\WOW6432Node\Blizzard Entertainment\Battle.net\Capabilities",
            @"SOFTWARE\Blizzard Entertainment\Battle.net\Capabilities"
        ];

        foreach (string keyPath in keys)
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
            string? installDir = key?.GetValue("ApplicationIcon") as string;
            if (installDir is null)
                continue;

            // ApplicationIcon value is something like "C:\Program Files (x86)\Battle.net\Battle.net.exe,0"
            int comma = installDir.IndexOf(',');
            string exePath = comma >= 0 ? installDir[..comma] : installDir;
            string? dir = Path.GetDirectoryName(exePath);
            if (dir is null)
                continue;

            // Game libraries are not stored in the Battle.net app dir — check game-specific keys
        }

        // Individual Blizzard game installs register their install paths
        string[] gameKeys =
        [
            @"SOFTWARE\WOW6432Node\Blizzard Entertainment",
            @"SOFTWARE\Blizzard Entertainment"
        ];

        foreach (string keyPath in gameKeys)
        {
            using RegistryKey? parentKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (parentKey is null)
                continue;

            foreach (string gameName in parentKey.GetSubKeyNames())
            {
                if (gameName.Equals("Battle.net", StringComparison.OrdinalIgnoreCase))
                    continue;

                using RegistryKey? gameKey = parentKey.OpenSubKey(gameName);
                string? installPath = gameKey?.GetValue("InstallPath") as string;
                if (installPath is null || !Directory.Exists(installPath))
                    continue;

                string? parent = Directory.GetParent(installPath)?.FullName;
                if (parent is not null && Directory.Exists(parent))
                    installRoots.Add(parent);
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in installRoots)
            result[root] = "Battle.NET";

        return result;
    }
}
