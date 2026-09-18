using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Levels page's Back/Next page buttons, paging through 1-10 / 11-20 / bonus 21-25 /
/// bonus 26-35. The original 5x2 button grid was sized exactly for 10 levels, so each additional
/// batch of levels got its own page rather than a cramped single grid. Attach anywhere convenient
/// (doesn't need to be on either button itself) and wire nextButton/prevButton in the Inspector.
/// </summary>
public class LevelsPageToggle : MonoBehaviour
{
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;
    [SerializeField] private GameObject page4;
    [SerializeField] private TMPro.TMP_Text label;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    private GameObject[] Pages => new[] { page1, page2, page3, page4 };
    private static readonly string[] Labels = { "LEVELS 1-10", "LEVELS 11-20", "LEVELS 21-25", "BONUS 26-35" };

    /// <summary>Set by GameManager right before loading this scene when a level finishes but
    /// Act 2 isn't purchased yet, so the player lands straight on the page showing the
    /// locked-for-purchase level instead of having to find it themselves. Consumed (reset to
    /// false) on read so it only affects the very next time this scene loads.</summary>
    public static bool OpenOnPurchasePageNext;

    /// <summary>Index of the page that holds the Act 2 paywalled levels (26-35) - where
    /// OpenOnPurchasePageNext lands the player. Kept in sync with LevelProgress.Act2FirstLevel's
    /// page (currently page4, index 3).</summary>
    private const int PurchasePageIndex = 3;

    private int currentPage;

    private void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (prevButton != null) prevButton.onClick.AddListener(Previous);
    }

    private void Start()
    {
        if (OpenOnPurchasePageNext)
        {
            currentPage = PurchasePageIndex;
            OpenOnPurchasePageNext = false;
        }
        Apply();
    }

    private void Next()
    {
        var pages = Pages;
        currentPage = (currentPage + 1) % pages.Length;
        Apply();
    }

    private void Previous()
    {
        var pages = Pages;
        currentPage = (currentPage - 1 + pages.Length) % pages.Length;
        Apply();
    }

    private void Apply()
    {
        var pages = Pages;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == currentPage);
        }
        if (label != null) label.text = Labels[currentPage];
    }
}
