using UnityEngine;

/// <summary>
/// Attach to the level's exit/goal object with a Collider2D set to "Is Trigger".
/// Stays visually closed and inert until GameManager reports all coins collected, then
/// swaps to its "open" sprite and completes the level on contact.
/// Assign closedSprite / openSprite in the Inspector; if openSprite is left unassigned,
/// the object's current SpriteRenderer sprite is used as the open state.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    private SpriteRenderer spriteRenderer;
    private bool isOpen;

    // Same double-fire guard as CoinPickup/LifePickup - without it, two overlapping player
    // colliders would call CompleteLevel() twice in one frame and start two "load next level"
    // coroutines.
    private bool completing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (openSprite == null) openSprite = spriteRenderer.sprite;
        if (closedSprite != null) spriteRenderer.sprite = closedSprite;
    }

    private void Update()
    {
        bool shouldBeOpen = GameManager.Instance != null && GameManager.Instance.AllCoinsCollected;
        if (shouldBeOpen == isOpen) return;

        isOpen = shouldBeOpen;
        Sprite target = isOpen ? openSprite : closedSprite;
        if (target != null) spriteRenderer.sprite = target;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completing) return;
        if (!isOpen) return;
        if (other.GetComponent<PlayerMovementScript>() == null) return;

        completing = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }
}
