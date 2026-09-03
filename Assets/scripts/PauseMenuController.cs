using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the phone's hardware back button during gameplay. Unity's Input System maps
/// Android's back button to Keyboard.escapeKey, so without this, pressing back on a Level
/// scene falls through to Android's default behavior and silently kills the app. Instead,
/// this opens a Resume / Exit confirmation panel; pressing back again while it's open resumes.
///
/// Lives once per level scene (see LevelX.unity -> UI/Canvas/PauseMenu), driven by a
/// CanvasGroup like PurchasePromptUI, and freezes gameplay via Time.timeScale while open -
/// guarded so it never opens over an already-paused state (level complete / continue-with-ad),
/// which also drive Time.timeScale to 0f.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string levelsSceneName = "Levels";

    private CanvasGroup canvasGroup;
    private bool isOpen;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (isOpen)
        {
            Resume();
            return;
        }

        // Don't open over another panel that's already paused the game (level complete,
        // continue-with-ad) - let that flow own the timescale until it's done with it.
        if (Time.timeScale == 0f) return;

        Open();
    }

    private void Open()
    {
        isOpen = true;
        Time.timeScale = 0f;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Wired to the "Resume" button's OnClick in the inspector (and the overlay's).</summary>
    public void Resume()
    {
        isOpen = false;
        Time.timeScale = 1f;
        Hide();
    }

    /// <summary>Wired to the "Exit" button's OnClick in the inspector.</summary>
    public void ExitToLevels()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelsSceneName);
    }
}
