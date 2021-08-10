using System;
using System.Collections.Concurrent;
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

        #region Main Method

        public static async Task Main(string[] args)
        {
            if (ShouldCleanupOrphanedDirectory(args))
            {
                return;
            }

            DriveInfo driveInfo = new("D:\\");

            Console.WriteLine();
            Console.WriteLine("         Total Space: {0}", FormatBytes(driveInfo.TotalSize));
            Console.WriteLine("          Used Space: {0}", FormatBytes(driveInfo.TotalSize - driveInfo.AvailableFreeSpace));
            Console.WriteLine("Available Free Space: {0}", FormatBytes(driveInfo.AvailableFreeSpace));
            Console.WriteLine();

            IEnumerable<string> gameRootDirs = GetGameRootDirectories().Where(d => Directory.Exists(d));
            ConcurrentBag<GameInfo[]> bag = new();
            IEnumerable<Task> tasks = gameRootDirs.Select(async item =>
            {
                bag.Add(await GetGames(item).ConfigureAwait(false));
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);

            IEnumerable<GameInfo> games = bag.SelectMany(b => b);

            foreach (GameInfo gameInfo in games.OrderByDescending(g => g.GameSize))
            {
                int leftPadding = games.Max(g => g.Name.Length) + 10;
                Console.WriteLine("{0}: {1} [{2}]", gameInfo.Name.PadLeft(leftPadding), FormatBytes(gameInfo.GameSize).PadRight(10), GetPercentage(gameInfo.GameSize, driveInfo.TotalSize));
            }

            Console.WriteLine();
        }

        #endregion

        #region Helper Methods

        private static Task<GameInfo[]> GetGames(string directory)
        {
            List<GameInfo> games = new List<GameInfo>();

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            foreach (DirectoryInfo dir in directoryInfo.EnumerateDirectories())
            {
                long totalSize = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
                games.Add(new GameInfo(directory, dir.Name, totalSize));
            }

            return Task.FromResult(games.ToArray());
        }

        private static string FormatBytes(long bytes)
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

        private static string[] GetGameRootDirectories()
        {
            List<string> directories = new()
            {
                @"D:\Battlet_Net",
                @"D:\Epic",
                @"D:\GOG_Galaxy\Games",
                @"D:\origin_games",
                @"D:\Steam\steamapps\common",
                @"D:\Ubisoft_Connect"
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

        private class GameInfo
        {

            public GameInfo(string parentDir, string name, long gameSize)
            {
                ParentDir = parentDir;
                Name = name;
                GameSize = gameSize;
            }

            public long GameSize { get; private set; }
            public string Name { get; private set; }
            public string ParentDir { get; private set; }

        }

        #endregion

    }
}
