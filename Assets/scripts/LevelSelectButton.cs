using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One button on the Levels page. Reads LevelProgress on enable to decide whether this level
/// is playable yet, dims + disables it if not, and shows a checkmark badge if it's already
/// been completed. Clicking it (only possible when unlocked) loads "Level{levelNumber}".
///
/// Levels 11+ ("Act 2") reuse the same lock icon, but with a twist: if the reason it's locked
/// is "not purchased" rather than "haven't finished the previous level", the button stays
/// tappable and a tap starts the Act 2 purchase (see IAPManager.BuyAct2) instead of doing
/// nothing - no separate "Buy" UI needed.
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
    private bool needsPurchase;

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
        LevelProgress.OnAct2PurchaseChanged += Refresh;
    }

    private void OnDisable()
    {
        LevelProgress.OnAct2PurchaseChanged -= Refresh;
    }

    private void Refresh()
    {
        bool unlocked = LevelProgress.IsUnlocked(levelNumber);
        bool completed = LevelProgress.IsCompleted(levelNumber);

        // Distinct from "locked, do nothing": this level's only blocker is the Act 2 purchase,
        // so tapping it (still dimmed, same lock icon) should offer to buy instead of sitting dead.
        needsPurchase = !unlocked
            && levelNumber >= LevelProgress.Act2FirstLevel
            && !LevelProgress.IsAct2Purchased();

        button.interactable = unlocked || needsPurchase;
        canvasGroup.alpha = unlocked ? 1f : lockedAlpha;

        if (lockIcon != null) lockIcon.SetActive(!unlocked);
        if (completeCheckmark != null) completeCheckmark.SetActive(unlocked && completed);
    }

    private void OnClick()
    {
        if (LevelProgress.IsUnlocked(levelNumber))
        {
            SceneManager.LoadScene("Level" + levelNumber);
            return;
        }

        if (needsPurchase && IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyAct2();
        }
    }
}
