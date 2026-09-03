using UnityEngine;

/// <summary>
/// Confirmation popup shown before starting the Act 2 purchase flow, so tapping a locked
/// Levels 11-20 button doesn't drop the player straight into Google Play's payment sheet.
/// Lives on the Levels scene's UI canvas (see Levels.unity) as an always-present, always-active
/// object toggled via a CanvasGroup rather than SetActive, so Instance is registered as soon as
/// the scene loads (LevelSelectButton may call Show() before this would otherwise wake up).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PurchasePromptUI : MonoBehaviour
{
    public static PurchasePromptUI Instance { get; private set; }

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Wired to the "Unlock" button's OnClick in the inspector.</summary>
    public void OnConfirmClicked()
    {
        Hide();
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyAct2();
        }
    }

    /// <summary>Wired to the "Not Now" button's OnClick in the inspector.</summary>
    public void OnCancelClicked()
    {
        Hide();
    }
}
