namespace GameSpace.Detectors;

/// <summary>
/// Scans fixed drives for directories that look like game library roots.
/// A candidate root is a top-level folder whose children each contain at
/// least one .exe and at least one known game artifact.
/// </summary>
internal static class HeuristicDetector
{
    private static readonly string[] GameArtifacts =
    [
        "steam_api.dll", "steam_api64.dll",
        "EOSSDK-Win64-Shipping.dll", "EOSSDK-Win32-Shipping.dll",
        "galaxy.dll", "galaxy64.dll",                    // GOG
        "uplay_r1.dll", "uplay_r164.dll",               // Ubisoft
        "engine.ini", "DefaultEngine.ini",               // Unreal
        "UnityPlayer.dll",                               // Unity
        "d3d11.dll", "d3d12.dll", "dxgi.dll",
    ];

    private static readonly HashSet<string> GameArtifactSet =
        new(GameArtifacts, StringComparer.OrdinalIgnoreCase);

    // Minimum size in bytes for a subdirectory to be considered a game (100 MB)
    private const long MinGameSizeBytes = 100L * 1024 * 1024;

    // Top-level folder names to skip entirely
    private static readonly HashSet<string> ExcludedTopLevelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "Users", "Program Files", "Program Files (x86)",
        "$Recycle.Bin", "System Volume Information", "ProgramData",
        "Recovery", "Boot", "EFI"
    };

    public static Dictionary<string, string> Detect(IReadOnlyCollection<string> alreadyKnownRoots)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            foreach (DirectoryInfo topDir in SafeEnumerateDirs(drive.RootDirectory))
            {
                if (ExcludedTopLevelNames.Contains(topDir.Name))
                    continue;

                if (alreadyKnownRoots.Any(r => r.StartsWith(topDir.FullName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (LooksLikeGameLibraryRoot(topDir))
                    result[topDir.FullName] = "Unknown";
            }
        }

        return result;
    }

    private static bool LooksLikeGameLibraryRoot(DirectoryInfo dir)
    {
        int gamelikeChildren = 0;
        int totalChildren = 0;

        foreach (DirectoryInfo child in SafeEnumerateDirs(dir))
        {
            totalChildren++;
            if (LooksLikeGame(child))
                gamelikeChildren++;

            // Short-circuit: enough evidence already
            if (gamelikeChildren >= 3)
                return true;
        }

        // At least 2 game-like children, and majority are games
        return gamelikeChildren >= 2 && gamelikeChildren >= totalChildren / 2;
    }

    private static bool LooksLikeGame(DirectoryInfo dir)
    {
        try
        {
            bool hasExe = false;
            bool hasArtifact = false;
            long size = 0;

            foreach (FileInfo file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;

                if (!hasExe && file.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    hasExe = true;

                if (!hasArtifact && GameArtifactSet.Contains(file.Name))
                    hasArtifact = true;

                if (hasExe && hasArtifact && size >= MinGameSizeBytes)
                    return true;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return false;
    }

    private static IEnumerable<DirectoryInfo> SafeEnumerateDirs(DirectoryInfo dir)
    {
        try
        {
            return dir.EnumerateDirectories();
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }
}
