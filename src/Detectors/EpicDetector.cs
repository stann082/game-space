using System.Text.Json;

namespace GameSpace.Detectors;

internal static class EpicDetector
{
    public static Dictionary<string, string> Detect()
    {
        string manifestsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Epic\EpicGamesLauncher\Data\Manifests");

        if (!Directory.Exists(manifestsDir))
            return [];

        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string manifestFile in Directory.EnumerateFiles(manifestsDir, "*.item"))
        {
            try
            {
                string json = File.ReadAllText(manifestFile);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("InstallLocation", out JsonElement location))
                    continue;

                string? installPath = location.GetString();
                if (installPath is null || !Directory.Exists(installPath))
                    continue;

                // The parent of the install location is the library root
                string? parent = Directory.GetParent(installPath)?.FullName;
                if (parent is not null && Directory.Exists(parent))
                    installRoots.Add(parent);
            }
            catch (Exception)
            {
                // skip malformed manifests
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string libraryRoot in installRoots)
            result[libraryRoot] = "Epic Games";

        return result;
    }
}
