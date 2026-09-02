using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central point for score, death/respawn, and level-complete flow.
///
/// Setup: create an empty GameObject named "GameManager" in the scene and add this
/// component. scoreText / levelCompletePanel are both optional - leave them unassigned
/// and the game still works (falls back to Debug.Log), assign them once you've built
/// the HUD / end-of-level UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI (optional - safe to leave unassigned)")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Timing")]
    [SerializeField] private float deathRestartDelay = 1.5f;

    [Header("Coins")]
    [SerializeField] private int totalCoins = 3;
    [SerializeField] private TMP_Text coinText;

    [Header("Lives")]
    [SerializeField] private int livesPerLevel = 3;
    [SerializeField] private TMP_Text livesText;

    [Header("Checkpoint")]
    [Tooltip("How long (seconds) the player is immune to hazards right after respawning at a " +
        "checkpoint - gives them a beat to move before an enemy that happens to be nearby can " +
        "kill them again immediately.")]
    [SerializeField] private float respawnInvulnerability = 1.5f;

    [Header("Level Progression")]
    [Tooltip("Scene to load when this level's exit is reached. Leave blank for the last " +
        "level in the game - CompleteLevel() then falls back to the pause + levelCompletePanel behavior.")]
    [SerializeField] private string nextSceneName = "";

    [Header("Continue with Ad (optional - safe to leave unassigned)")]
    [Tooltip("Shown when lives run out, offering a rewarded ad for one more life instead of " +
        "going straight to Game Over. Needs a \"Watch Ad\" button wired to WatchAdForExtraLife() " +
        "and a \"No Thanks\" button wired to DeclineContinue(). Falls back straight to the normal " +
        "game-over flow if left unassigned, or if AdsManager has no ad ready yet - never blocks " +
        "the player waiting on an ad that isn't there.")]
    [SerializeField] private GameObject continuePanel;

    // Static so a death's scene reload doesn't wipe the count (a fresh GameManager.Awake()
    // runs every reload) - but still resets to a full set each time the game itself is
    // actually (re)started, since static fields reinitialize on domain reload. int.MinValue
    // is a sentinel meaning "not seeded yet this run".
    private static int livesRemaining = int.MinValue;

    // Checkpoint state - static for the same reload-survival reason as livesRemaining above.
    // checkpointSceneName guards against a checkpoint from one level ever applying to a
    // different one (e.g. stale state if something odd happens with scene load order);
    // it's also cleared explicitly on every level-complete/game-over transition below.
    private static bool hasCheckpoint;
    private static string checkpointSceneName;
    private static Vector3 checkpointPosition;
    private static int checkpointCoinsCollected;

    private int score;

    public int CoinsCollected { get; private set; }
    public bool AllCoinsCollected => CoinsCollected >= totalCoins;
    public int LivesRemaining => livesRemaining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Defensive reset: the continue-panel flow below pauses via Time.timeScale = 0f, same
        // as the existing levelCompletePanel path - if a scene ever loads while that's still
        // zero (e.g. quitting out mid-pause), nothing else resets it, so every level would come
        // up frozen. Cheap enough to just always reset it here.
        Time.timeScale = 1f;

        if (livesRemaining == int.MinValue)
        {
            livesRemaining = livesPerLevel;
        }

        ApplyCheckpointIfAny();

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateCoinsUI();
    }

    /// <summary>Called by Checkpoint when the player first touches it. Remembers where to
    /// respawn on a future death within this same level, and locks in coin progress so
    /// already-collected coins aren't lost on respawn.</summary>
    public void SetCheckpoint(Vector3 position)
    {
        hasCheckpoint = true;
        checkpointSceneName = SceneManager.GetActiveScene().name;
        checkpointPosition = position;
        checkpointCoinsCollected = CoinsCollected;
    }

    private void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointSceneName = null;
    }

    /// <summary>If a checkpoint was set earlier in this same level, moves the just-spawned
    /// player there and restores coin progress. Runs from Awake(), before any Start() has
    /// fired, so this happens before the player or camera do anything with the default
    /// scene position.</summary>
    private void ApplyCheckpointIfAny()
    {
        if (!hasCheckpoint || checkpointSceneName != SceneManager.GetActiveScene().name)
        {
            return;
        }

        CoinsCollected = checkpointCoinsCollected;
        UpdateCoinsUI();

        var player = FindFirstObjectByType<PlayerMovementScript>();
        if (player == null) return;

        player.transform.position = checkpointPosition;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = checkpointPosition;
            rb.linearVelocity = Vector2.zero;
        }

        player.GrantInvulnerability(respawnInvulnerability);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    /// <summary>Called by CoinPickup when the player collects a coin. Tracks progress toward
    /// unlocking the level exit (see LevelExit) in addition to scoring.</summary>
    public void CollectCoin(int value)
    {
        CoinsCollected++;
        AddScore(value);
        UpdateCoinsUI();
    }

    /// <summary>Called by LifePickup when the player collects a rare extra-life item.</summary>
    public void AddLife(int amount = 1)
    {
        livesRemaining += amount;
        UpdateLivesUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateCoinsUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected}/{totalCoins}";
        }
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = $"x {livesRemaining}";
        }
    }

    /// <summary>Called by PlayerMovementScript once the death animation starts. Spends one
    /// life; while lives remain, restarts this level like before. Once they run out, sends
    /// the player back to the Start Menu instead and refills lives for their next attempt.</summary>
    public void PlayerDied()
    {
        livesRemaining--;
        UpdateLivesUI();

        if (livesRemaining > 0)
        {
            StartCoroutine(RestartAfterDelay());
            return;
        }

        bool canOfferContinue = continuePanel != null
            && AdsManager.Instance != null
            && AdsManager.Instance.IsRewardedAdReady;

        StartCoroutine(canOfferContinue ? ShowContinuePanelAfterDelay() : GameOverAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ShowContinuePanelAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        continuePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>Wired to the continue panel's "Watch Ad" button. Only grants the extra life
    /// once AdsManager's own callback confirms the ad was actually watched through - see
    /// AdsManager.ShowRewardedAd.</summary>
    public void WatchAdForExtraLife()
    {
        if (AdsManager.Instance == null)
        {
            DeclineContinue();
            return;
        }

        AdsManager.Instance.ShowRewardedAd(() =>
        {
            livesRemaining = 1;
            UpdateLivesUI();
            if (continuePanel != null) continuePanel.SetActive(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    /// <summary>Wired to the continue panel's "No Thanks" button - proceeds to the normal
    /// game-over flow instead.</summary>
    public void DeclineContinue()
    {
        if (continuePanel != null) continuePanel.SetActive(false);
        Time.timeScale = 1f;
        StartCoroutine(GameOverAfterDelay());
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        livesRemaining = livesPerLevel;
        ClearCheckpoint();
        SceneManager.LoadScene("Start Menu");
    }

    /// <summary>Called by LevelExit when the player reaches the goal. Moves on to
    /// nextSceneName if one is assigned; otherwise falls back to the pause + panel
    /// behavior (for the last level in the game, until a real end screen exists).</summary>
    public void CompleteLevel()
    {
        var levelNumber = LevelProgress.ParseLevelNumber(SceneManager.GetActiveScene().name);
        if (levelNumber.HasValue)
        {
            LevelProgress.MarkCompleted(levelNumber.Value);
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.NotifyLevelCompleted();
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            StartCoroutine(LoadNextLevelAfterDelay());
            return;
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        else
        {
            Debug.Log("Level Complete! (assign levelCompletePanel or nextSceneName on GameManager)");
        }

        Time.timeScale = 0f;
    }

    private IEnumerator LoadNextLevelAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        ClearCheckpoint();
        SceneManager.LoadScene(nextSceneName);
    }
}
