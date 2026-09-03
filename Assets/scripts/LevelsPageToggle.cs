using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles between the Levels page's two pages (1-10 / 11-20). The original 5x2 button grid
/// was sized exactly for 10 levels, so 11-20 got a second page rather than a cramped 20-button
/// grid. Attach to the toggle button itself; flips its own label to say which page tapping it
/// goes to next, same self-wiring pattern as LevelSelectButton (AddListener in Awake).
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelsPageToggle : MonoBehaviour
{
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private TMPro.TMP_Text label;

    /// <summary>Set by GameManager right before loading this scene when Level10 finishes but
    /// Act 2 isn't purchased yet, so the player lands straight on the page showing Level11
    /// locked-for-purchase instead of having to find the toggle themselves. Consumed (reset to
    /// false) on read so it only affects the very next time this scene loads.</summary>
    public static bool OpenOnPage2Next;

    private bool onPage2;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void Start()
    {
        if (OpenOnPage2Next)
        {
            onPage2 = true;
            OpenOnPage2Next = false;
        }
        Apply();
    }

    private void Toggle()
    {
        onPage2 = !onPage2;
        Apply();
    }

    private void Apply()
    {
        if (page1 != null) page1.SetActive(!onPage2);
        if (page2 != null) page2.SetActive(onPage2);
        if (label != null) label.text = onPage2 ? "LEVELS 1-10" : "LEVELS 11-20";
    }
}
