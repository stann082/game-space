using Microsoft.Win32;

namespace GameSpace.Detectors;

internal static class XboxDetector
{
    public static Dictionary<string, string> Detect()
    {
        var installRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectFromRegistry(installRoots);
        CollectFromGamingRoots(installRoots);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in installRoots)
            result[root] = "XBOX Games";

        return result;
    }

    private static void CollectFromRegistry(HashSet<string> roots)
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\GamingServices\PackageRepository\Root");

        if (key is null)
            return;

        foreach (string name in key.GetValueNames())
        {
            string? path = key.GetValue(name) as string;
            if (path is not null && Directory.Exists(path))
                roots.Add(path);
        }
    }

    private static void CollectFromGamingRoots(HashSet<string> roots)
    {
        // Xbox App writes a GamingRoot file at the root of each configured library drive
        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
        {
            string gamingRoot = Path.Combine(drive.RootDirectory.FullName, "XboxGames");
            if (Directory.Exists(gamingRoot))
                roots.Add(gamingRoot);

            // Some setups use a GamingRoot marker file
            string markerFile = Path.Combine(drive.RootDirectory.FullName, ".GamingRoot");
            if (!File.Exists(markerFile))
                continue;

            try
            {
                // The file contains a null-terminated UTF-16 path
                byte[] bytes = File.ReadAllBytes(markerFile);
                string path = System.Text.Encoding.Unicode.GetString(bytes).TrimEnd('\0').Trim();
                if (Directory.Exists(path))
                    roots.Add(path);
            }
            catch (Exception)
            {
                // ignore unreadable marker files
            }
        }
    }
}
