using System.Globalization;

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
        if (ShouldCleanupOrphanedDirectory(args))
        {
            return;
        }

        DriveInfo driveInfo = new("D:\\");

        Console.WriteLine();
        Console.WriteLine($"         Total Space: {ByteFormatter.Format(driveInfo.TotalSize)}");
        Console.WriteLine($"          Used Space: {ByteFormatter.Format(driveInfo.TotalSize - driveInfo.AvailableFreeSpace)}");
        Console.WriteLine($"Available Free Space: {ByteFormatter.Format(driveInfo.AvailableFreeSpace)}");
        Console.WriteLine();

        List<Task<GameInfo>> tasks = new();

        Dictionary<string, string> gameRootDirs = GetGameRootDirectories();
        List<DirectoryInfo> allGameDirs = new List<DirectoryInfo>();
        foreach (var gameRootDir in gameRootDirs.Where(d => Directory.Exists(d.Key)))
        {
            IEnumerable<DirectoryInfo> gameDirs = GetAllGameDirs(gameRootDir.Key);
            allGameDirs.AddRange(gameDirs);
        }
        
        Parallel.ForEach(allGameDirs, dirInfo => { tasks.Add(GetGameInfo(dirInfo)); });
        GameInfo[] games = await Task.WhenAll(tasks.ToArray());
        
        int maxNameLength = games.Max(g => g.Name.Length);
        int maxPlatformLength = gameRootDirs.Values.Max(p => p.Length);
        int maxSizeLength = games.Max(g => g.GameSizeFormatted.Length);
        foreach (GameInfo gameInfo in games.OrderByDescending(g => g.GameSizeBytes))
        {
            string name = gameInfo.Name.PadLeft(maxNameLength);
            string size = gameInfo.GameSizeFormatted.PadRight(maxSizeLength);
            string percentage = GetPercentage(gameInfo.GameSizeBytes, driveInfo.TotalSize);
            string parentDir = Directory.GetParent(gameInfo.DirPath)?.FullName;
            string platform = gameRootDirs[parentDir ?? throw new InvalidOperationException($"Invalid key for {gameInfo.DirPath}")].PadLeft(maxPlatformLength);
            Console.WriteLine($"{name} ({platform}): {size} [{percentage}]");
        }

        if (_exceptionMessages.Count > 0)
        {
            Console.WriteLine();
            foreach (string exceptionMessage in _exceptionMessages)
            {
                Console.WriteLine(exceptionMessage);
            }
        }

        Console.WriteLine();
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<DirectoryInfo> GetAllGameDirs(string gameRootDir)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(gameRootDir);
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

    private static Dictionary<string, string> GetGameRootDirectories()
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            [@"D:\Battle_Net"] = "Battle.NET",
            [@"D:\EA Games"] = "EA Games",
            [@"D:\EA_Games"] = "EA Games",
            [@"D:\EpicGames"] = "Epic Games",
            [@"D:\GOG_Galaxy\Games"] = "GOG Galaxy",
            [@"D:\SteamLibrary\steamapps\common"] = "Steam",
            [@"D:\Ubisoft"] = "Ubisoft",
            [@"D:\XboxGames"] = "XBOX Games"
        };
        return map;
    }

    private static string GetPercentage(long gameSize, long totalDiskSize)
    {
        NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
        nfi.PercentDecimalDigits = 0;
        double percent = gameSize / (double)totalDiskSize;
        return percent.ToString("P", nfi);
    }

    private static bool ShouldCleanupOrphanedDirectory(string[] args)
    {
        if (!args.Contains(CleanupArgument))
        {
            return false;
        }

        string gameFolderToDelete = string.Join(' ', args.Where(a => a != CleanupArgument));
        foreach (string directory in GetGameRootDirectories().Select(c => c.Key))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

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
