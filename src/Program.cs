using System.Globalization;
using GameSpace.Detectors;

namespace GameSpace;

public static class Program
{

    #region Constants

    private const string CleanupArgument = "--cleanup";

    #endregion

    #region Variables

    private static readonly IList<string> _exceptionMessages = new List<string>();

    #endregion

    #region Main Method

    public static async Task Main(string[] args)
    {
        Dictionary<string, string> gameRootDirs = GameLibraryDetector.Detect();

        if (ShouldCleanupOrphanedDirectory(args, gameRootDirs))
        {
            return;
        }

        PrintDriveInfo(gameRootDirs.Keys);

        List<DirectoryInfo> allGameDirs = new();
        foreach (var (path, _) in gameRootDirs.Where(d => Directory.Exists(d.Key)))
        {
            allGameDirs.AddRange(GetAllGameDirs(path));
        }

        if (allGameDirs.Count == 0)
        {
            Console.WriteLine("No game directories detected.");
            return;
        }

        List<Task<GameInfo>> tasks = new();
        Parallel.ForEach(allGameDirs, dirInfo => { tasks.Add(GetGameInfo(dirInfo)); });
        GameInfo[] games = await Task.WhenAll(tasks.ToArray());

        int maxNameLength = games.Max(g => g.Name.Length);
        int maxPlatformLength = gameRootDirs.Values.Max(p => p.Length);
        int maxSizeLength = games.Max(g => g.GameSizeFormatted.Length);

        // Build a lookup from game dir → platform label for display
        var dirToLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rootPath, label) in gameRootDirs)
        {
            foreach (DirectoryInfo gameDir in allGameDirs.Where(d =>
                string.Equals(Directory.GetParent(d.FullName)?.FullName, rootPath, StringComparison.OrdinalIgnoreCase)))
            {
                dirToLabel.TryAdd(gameDir.FullName, label);
            }
        }

        // Use the drive containing the largest single game for the percentage denominator
        long totalDiskSize = GetTotalDiskSize(games);

        foreach (GameInfo gameInfo in games.OrderByDescending(g => g.GameSizeBytes))
        {
            string name = gameInfo.Name.PadLeft(maxNameLength);
            string size = gameInfo.GameSizeFormatted.PadRight(maxSizeLength);
            string percentage = GetPercentage(gameInfo.GameSizeBytes, totalDiskSize);
            string platform = (dirToLabel.GetValueOrDefault(gameInfo.DirPath) ?? "Unknown").PadLeft(maxPlatformLength);
            Console.WriteLine($"{name} ({platform}): {size} [{percentage}]");
        }

        if (_exceptionMessages.Count > 0)
        {
            Console.WriteLine();
            foreach (string exceptionMessage in _exceptionMessages)
                Console.WriteLine(exceptionMessage);
        }

        Console.WriteLine();
    }

    #endregion

    #region Helper Methods

    private static void PrintDriveInfo(IEnumerable<string> libraryRoots)
    {
        var drives = libraryRoots
            .Select(p => Path.GetPathRoot(p))
            .Where(r => r is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(r => { try { return new DriveInfo(r!); } catch { return null; } })
            .Where(d => d is not null)
            .ToList();

        Console.WriteLine();
        foreach (DriveInfo drive in drives!)
        {
            Console.WriteLine($"Drive {drive.Name}");
            Console.WriteLine($"         Total Space: {ByteFormatter.Format(drive.TotalSize)}");
            Console.WriteLine($"          Used Space: {ByteFormatter.Format(drive.TotalSize - drive.AvailableFreeSpace)}");
            Console.WriteLine($"Available Free Space: {ByteFormatter.Format(drive.AvailableFreeSpace)}");
            Console.WriteLine();
        }
    }

    private static long GetTotalDiskSize(GameInfo[] games)
    {
        if (games.Length == 0)
            return 1;

        // Pick the drive that holds the most game data as the reference
        var driveGroups = games
            .GroupBy(g => Path.GetPathRoot(g.DirPath), StringComparer.OrdinalIgnoreCase)
            .Select(grp =>
            {
                try
                {
                    return new DriveInfo(grp.Key!).TotalSize;
                }
                catch
                {
                    return 0L;
                }
            });

        return driveGroups.Max();
    }

    private static IEnumerable<DirectoryInfo> GetAllGameDirs(string gameRootDir)
    {
        DirectoryInfo directoryInfo = new(gameRootDir);
        try
        {
            return directoryInfo.EnumerateDirectories();
        }
        catch (UnauthorizedAccessException ex)
        {
            _exceptionMessages.Add(ex.Message);
            return [];
        }
    }

    private static Task<GameInfo> GetGameInfo(DirectoryInfo dir)
    {
        long totalSize = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        return Task.FromResult(new GameInfo(dir.Name, totalSize, dir.FullName));
    }

    private static string GetPercentage(long gameSize, long totalDiskSize)
    {
        NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
        nfi.PercentDecimalDigits = 0;
        double percent = gameSize / (double)totalDiskSize;
        return percent.ToString("P", nfi);
    }

    private static bool ShouldCleanupOrphanedDirectory(string[] args, Dictionary<string, string> gameRootDirs)
    {
        if (!args.Contains(CleanupArgument))
            return false;

        string gameFolderToDelete = string.Join(' ', args.Where(a => a != CleanupArgument));
        foreach (string directory in gameRootDirs.Keys)
        {
            if (!Directory.Exists(directory))
                continue;

            string[] dirs = Directory.GetDirectories(directory, $"{gameFolderToDelete}", SearchOption.AllDirectories);
            switch (dirs.Length)
            {
                case 0:
                    continue;
                case > 1:
                    Console.WriteLine($"Found more than 1 directory matching {gameFolderToDelete} in {directory}...");
                    continue;
            }

            string foundDirectory = dirs[0];
            string[] exeFiles = Directory.GetFiles(foundDirectory, "*.exe", SearchOption.AllDirectories);
            if (exeFiles.Length > 0)
            {
                Console.WriteLine($"Some executable files were found. The directory {foundDirectory} may not be safe to remove...");
                break;
            }

            Console.WriteLine($"Removing {foundDirectory} directory...");
            Directory.Delete(foundDirectory, true);
            break;
        }

        return true;
    }

    #endregion

    #region Helper Classes

    private static class ByteFormatter
    {

        public static string Format(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return $"{dblSByte:0.##} {suffix[i]}";
        }

    }

    private class GameInfo(string name, long gameSize, string dirPath)
    {
        public string DirPath { get; } = dirPath;
        public long GameSizeBytes { get; } = gameSize;
        public string GameSizeFormatted => ByteFormatter.Format(GameSizeBytes);
        public string Name { get; } = name;

        public override string ToString()
        {
            return $"{Name} [{GameSizeFormatted}]";
        }

    }

    #endregion

}
