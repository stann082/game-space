using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class SteamDetector
{
    public static Dictionary<string, string> Detect()
    {
        string? steamPath = FindSteamPath();
        if (steamPath is null)
            return [];

        string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
            return [];

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string libraryRoot in ParseLibraryRoots(vdfPath))
        {
            string commonPath = Path.Combine(libraryRoot, "steamapps", "common");
            if (Directory.Exists(commonPath))
                result[commonPath] = "Steam";
        }

        return result;
    }

    private static string? FindSteamPath()
    {
        string[] registryKeys =
        [
            @"SOFTWARE\Valve\Steam",
            @"SOFTWARE\WOW6432Node\Valve\Steam"
        ];

        foreach (string key in registryKeys)
        {
            using RegistryKey? rk = Registry.LocalMachine.OpenSubKey(key);
            string? path = rk?.GetValue("InstallPath") as string;
            if (path is not null && Directory.Exists(path))
                return path;
        }

        // Common fallback locations
        string[] defaults =
        [
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam"
        ];

        return defaults.FirstOrDefault(Directory.Exists);
    }

    // Parses the "path" entries from libraryfolders.vdf (KeyValues text format)
    private static IEnumerable<string> ParseLibraryRoots(string vdfPath)
    {
        foreach (string line in File.ReadLines(vdfPath))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                continue;

            // Format: "path"		"D:\\SteamLibrary"
            int first = trimmed.IndexOf('"', 6);
            int last = trimmed.LastIndexOf('"');
            if (first >= 0 && last > first)
            {
                string path = trimmed[(first + 1)..last].Replace("\\\\", "\\");
                if (Directory.Exists(path))
                    yield return path;
            }
        }
    }
}
