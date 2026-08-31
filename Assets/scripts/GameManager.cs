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

    [Header("Level Progression")]
    [Tooltip("Scene to load when this level's exit is reached. Leave blank for the last " +
        "level in the game - CompleteLevel() then falls back to the pause + levelCompletePanel behavior.")]
    [SerializeField] private string nextSceneName = "";

    // Static so a death's scene reload doesn't wipe the count (a fresh GameManager.Awake()
    // runs every reload) - but still resets to a full set each time the game itself is
    // actually (re)started, since static fields reinitialize on domain reload. int.MinValue
    // is a sentinel meaning "not seeded yet this run".
    private static int livesRemaining = int.MinValue;

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

        if (livesRemaining == int.MinValue)
        {
            livesRemaining = livesPerLevel;
        }

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateCoinsUI();
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
            coinText.text = $"{CoinsCollected} / {totalCoins}";
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
        }
        else
        {
            StartCoroutine(GameOverAfterDelay());
        }
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        livesRemaining = livesPerLevel;
        SceneManager.LoadScene("Start Menu");
    }

    /// <summary>Called by LevelExit when the player reaches the goal. Moves on to
    /// nextSceneName if one is assigned; otherwise falls back to the pause + panel
    /// behavior (for the last level in the game, until a real end screen exists).</summary>
    public void CompleteLevel()
    {
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
        SceneManager.LoadScene(nextSceneName);
    }
}
