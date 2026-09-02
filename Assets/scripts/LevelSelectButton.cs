using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One button on the Levels page. Reads LevelProgress on enable to decide whether this level
/// is playable yet, dims + disables it if not, and shows a checkmark badge if it's already
/// been completed. Clicking it (only possible when unlocked) loads "Level{levelNumber}".
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour
{
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject completeCheckmark;
    [SerializeField] [Range(0f, 1f)] private float lockedAlpha = 0.4f;

    private Button button;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool unlocked = LevelProgress.IsUnlocked(levelNumber);
        bool completed = LevelProgress.IsCompleted(levelNumber);

        button.interactable = unlocked;
        canvasGroup.alpha = unlocked ? 1f : lockedAlpha;

        if (lockIcon != null) lockIcon.SetActive(!unlocked);
        if (completeCheckmark != null) completeCheckmark.SetActive(unlocked && completed);
    }

    private void OnClick()
    {
        if (!LevelProgress.IsUnlocked(levelNumber)) return;
        SceneManager.LoadScene("Level" + levelNumber);
    }
}
