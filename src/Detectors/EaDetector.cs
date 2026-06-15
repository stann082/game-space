using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class EaDetector
{
    public static Dictionary<string, string> Detect()
    {
        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectFromRegistry(installRoots);
        CollectFromManifests(installRoots);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in installRoots)
            result[root] = "EA Games";

        return result;
    }

    private static void CollectFromRegistry(HashSet<string> roots)
    {
        string[] keys =
        [
            @"SOFTWARE\WOW6432Node\EA Games",
            @"SOFTWARE\EA Games"
        ];

        foreach (string keyPath in keys)
        {
            using RegistryKey? parentKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (parentKey is null)
                continue;

            foreach (string gameName in parentKey.GetSubKeyNames())
            {
                using RegistryKey? gameKey = parentKey.OpenSubKey(gameName);
                string? installDir = gameKey?.GetValue("Install Dir") as string
                                  ?? gameKey?.GetValue("InstallDir") as string;

                if (installDir is null || !Directory.Exists(installDir))
                    continue;

                string? parent = Directory.GetParent(installDir)?.FullName;
                if (parent is not null && Directory.Exists(parent))
                    roots.Add(parent);
            }
        }
    }

    private static void CollectFromManifests(HashSet<string> roots)
    {
        // EA App stores __Installer\installerdata.xml inside each game folder.
        // We discover install roots via ProgramData manifests introduced in EA App 2023+.
        string manifestsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"EA Desktop\InstallData");

        if (!Directory.Exists(manifestsDir))
            return;

        foreach (string dir in Directory.EnumerateDirectories(manifestsDir))
        {
            string baseInstallDir = Path.Combine(dir, "base_install_path");
            if (!File.Exists(baseInstallDir))
                continue;

            string path = File.ReadAllText(baseInstallDir).Trim();
            if (!Directory.Exists(path))
                continue;

            string? parent = Directory.GetParent(path)?.FullName;
            if (parent is not null && Directory.Exists(parent))
                roots.Add(parent);
        }
    }
}
