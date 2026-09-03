using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Persistent (PlayerPrefs-backed) record of which levels have been completed, plus the
/// sequential-unlock rule the Levels page and GameManager both read: Level1 is always
/// unlocked, and LevelN (N > 1) unlocks once LevelN-1 is completed.
///
/// Levels 11-20 ("Act 2") additionally require the one-time Act 2 purchase - see
/// IsAct2Purchased/SetAct2Purchased, driven by IAPManager's verified purchase callback rather
/// than settable from anywhere else, so completion alone can't skip the paywall.
/// </summary>
public static class LevelProgress
{
    private const string CompletedKeyPrefix = "LevelCompleted_";
    private const string Act2PurchasedKey = "Act2Purchased";
    public const int Act2FirstLevel = 11;

    public static void MarkCompleted(int levelNumber)
    {
        PlayerPrefs.SetInt(CompletedKeyPrefix + levelNumber, 1);
        PlayerPrefs.Save();
    }

    public static bool IsCompleted(int levelNumber)
    {
        return PlayerPrefs.GetInt(CompletedKeyPrefix + levelNumber, 0) == 1;
    }

    /// <summary>True once IAPManager has confirmed the Act 2 purchase (fresh buy, or a restore
    /// on a new device/reinstall) - see IAPManager.OnPurchaseConfirmed.</summary>
    public static bool IsAct2Purchased()
    {
        return PlayerPrefs.GetInt(Act2PurchasedKey, 0) == 1;
    }

    /// <summary>Fires right after SetAct2Purchased changes the flag - LevelSelectButton
    /// subscribes so the Levels page updates immediately when a purchase completes, without
    /// needing to leave and re-enter the page.</summary>
    public static event Action OnAct2PurchaseChanged;

    public static void SetAct2Purchased(bool purchased)
    {
        PlayerPrefs.SetInt(Act2PurchasedKey, purchased ? 1 : 0);
        PlayerPrefs.Save();
        OnAct2PurchaseChanged?.Invoke();
    }

    public static bool IsUnlocked(int levelNumber)
    {
        if (levelNumber <= 1) return true;
        if (levelNumber >= Act2FirstLevel && !IsAct2Purchased()) return false;
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
