namespace GameSpace.Detectors;

internal static class GameLibraryDetector
{
    public static Dictionary<string, string> Detect()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Merge(result, SteamDetector.Detect());
        Merge(result, EpicDetector.Detect());
        Merge(result, GogDetector.Detect());
        Merge(result, EaDetector.Detect());
        Merge(result, XboxDetector.Detect());
        Merge(result, UbisoftDetector.Detect());
        Merge(result, BattleNetDetector.Detect());

        // Fall back to heuristic scan for any drives not already covered
        Merge(result, HeuristicDetector.Detect(result.Keys.ToList()));

        return result;
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> source)
    {
        foreach (var (path, label) in source)
        {
            if (!target.ContainsKey(path))
                target[path] = label;
        }
    }
}
