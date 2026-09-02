using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Persistent (PlayerPrefs-backed) record of which levels have been completed, plus the
/// sequential-unlock rule the Levels page and GameManager both read: Level1 is always
/// unlocked, and LevelN (N > 1) unlocks once LevelN-1 is completed.
/// </summary>
public static class LevelProgress
{
    private const string CompletedKeyPrefix = "LevelCompleted_";

    public static void MarkCompleted(int levelNumber)
    {
        PlayerPrefs.SetInt(CompletedKeyPrefix + levelNumber, 1);
        PlayerPrefs.Save();
    }

    public static bool IsCompleted(int levelNumber)
    {
        return PlayerPrefs.GetInt(CompletedKeyPrefix + levelNumber, 0) == 1;
    }

    public static bool IsUnlocked(int levelNumber)
    {
        if (levelNumber <= 1) return true;
        return IsCompleted(levelNumber - 1);
    }

    /// <summary>Pulls the trailing number out of a scene name like "Level7" -> 7. Returns
    /// null for scenes that aren't numbered levels (Start Menu, Levels, ...).</summary>
    public static int? ParseLevelNumber(string sceneName)
    {
        var match = Regex.Match(sceneName, @"Level(\d+)$");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        return null;
    }
}
