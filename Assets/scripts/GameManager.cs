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

    private int score;

    public int CoinsCollected { get; private set; }
    public bool AllCoinsCollected => CoinsCollected >= totalCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateScoreUI();
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
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    /// <summary>Called by PlayerMovementScript once the death animation starts.</summary>
    public void PlayerDied()
    {
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Called by LevelExit when the player reaches the goal.</summary>
    public void CompleteLevel()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        else
        {
            Debug.Log("Level Complete! (assign levelCompletePanel on GameManager for a real UI)");
        }

        Time.timeScale = 0f;
    }
}
