using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class UbisoftDetector
{
    public static Dictionary<string, string> Detect()
    {
        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] keys =
        [
            @"SOFTWARE\WOW6432Node\Ubisoft\Launcher",
            @"SOFTWARE\Ubisoft\Launcher"
        ];

        foreach (string keyPath in keys)
        {
            using RegistryKey? launcherKey = Registry.LocalMachine.OpenSubKey(keyPath);
            string? installsDir = launcherKey?.GetValue("InstallDir") as string;
            if (installsDir is not null && Directory.Exists(installsDir))
                installRoots.Add(installsDir);
        }

        // Ubisoft Connect also registers each game individually
        string[] gameKeys =
        [
            @"SOFTWARE\WOW6432Node\Ubisoft\Installs",
            @"SOFTWARE\Ubisoft\Installs"
        ];

        foreach (string keyPath in gameKeys)
        {
            using RegistryKey? installsKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (installsKey is null)
                continue;

            foreach (string gameId in installsKey.GetSubKeyNames())
            {
                using RegistryKey? gameKey = installsKey.OpenSubKey(gameId);
                string? installDir = gameKey?.GetValue("InstallDir") as string;
                if (installDir is null || !Directory.Exists(installDir))
                    continue;

                string? parent = Directory.GetParent(installDir)?.FullName;
                if (parent is not null && Directory.Exists(parent))
                    installRoots.Add(parent);
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in installRoots)
            result[root] = "Ubisoft";

        return result;
    }
}
