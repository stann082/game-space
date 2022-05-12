using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameSpace
{
    public static class Program
    {

        #region Constants

        private const string CLEANUP_ARGUMENT = "--cleanup";

        #endregion

        #region Variables

        private static IList<string> _exceptionMessages;

        #endregion

        #region Main Method

        public static async Task Main(string[] args)
        {
            _exceptionMessages = new List<string>();
            if (ShouldCleanupOrphanedDirectory(args))
            {
                return;
            }

            DriveInfo driveInfo = new("D:\\");

            Console.WriteLine();
            Console.WriteLine("         Total Space: {0}", ByteFormatter.Format(driveInfo.TotalSize));
            Console.WriteLine("          Used Space: {0}", ByteFormatter.Format(driveInfo.TotalSize - driveInfo.AvailableFreeSpace));
            Console.WriteLine("Available Free Space: {0}", ByteFormatter.Format(driveInfo.AvailableFreeSpace));
            Console.WriteLine();

            List<Task<GameInfo>> tasks = new();

            IEnumerable<string> gameRootDirs = GetGameRootDirectories().Where(d => Directory.Exists(d));
            IEnumerable<DirectoryInfo> allGameDirs = gameRootDirs.SelectMany(d => GetAllGameDirs(d));
            Parallel.ForEach(allGameDirs, dirInfo =>
            {
                tasks.Add(GetGameInfo(dirInfo));
            });

            GameInfo[] games = await Task.WhenAll(tasks.ToArray());
            foreach (GameInfo gameInfo in games.OrderByDescending(g => g.GameSizeBytes))
            {
                int leftPadding = games.Max(g => g.Name.Length) + 10;
                string name = gameInfo.Name.PadLeft(leftPadding);
                string size = gameInfo.GameSizeFormatted.PadRight(10);
                string percentage = GetPercentage(gameInfo.GameSizeBytes, driveInfo.TotalSize);
                Console.WriteLine($"{name}: {size} [{percentage}]");
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
                return Enumerable.Empty<DirectoryInfo>();
            }
        }

        private static Task<GameInfo> GetGameInfo(DirectoryInfo dir)
        {
            long totalSize = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return Task.FromResult(new GameInfo(dir.Name, totalSize));
        }

        private static string[] GetGameRootDirectories()
        {
            List<string> directories = new()
            {
                @"D:\Battle_Net",
                @"D:\EA_Games",
                @"D:\Epic",
                @"D:\GOG_Galaxy\Games",
                @"D:\origin_games",
                @"D:\Steam\steamapps\common",
                @"D:\Ubisoft_Connect",
                @"D:\WindowsApps"
            };

            return directories.ToArray();
        }

        private static string GetPercentage(long gameSize, long totalDiskSize)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.PercentDecimalDigits = 0;
            double percent = (double)gameSize / (double)totalDiskSize;
            return percent.ToString("P", nfi);
        }

        private static bool ShouldCleanupOrphanedDirectory(string[] args)
        {
            if (!args.Contains(CLEANUP_ARGUMENT))
            {
                return false;
            }

            string gameFolderToDelete = string.Join(' ', args.Where(a => a != CLEANUP_ARGUMENT));
            foreach (string directory in GetGameRootDirectories())
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                string[] dirs = Directory.GetDirectories(directory, $"{gameFolderToDelete}", SearchOption.AllDirectories);
                if (dirs.Length == 0)
                {
                    continue;
                }

                if (dirs.Length > 1)
                {
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

        private class GameInfo
        {

            public GameInfo(string name, long gameSize)
            {
                Name = name;
                GameSizeBytes = gameSize;
            }

            public long GameSizeBytes { get; private set; }
            public string GameSizeFormatted => ByteFormatter.Format(GameSizeBytes);
            public string Name { get; private set; }

            public override string ToString()
            {
                return $"{Name} [{GameSizeFormatted}]";
            }

        }

        #endregion

    }
}
