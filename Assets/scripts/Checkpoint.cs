using UnityEngine;

/// <summary>
/// Attach to a checkpoint marker with a Collider2D set to "Is Trigger". On first contact
/// with the player, records this checkpoint's position (and current coin progress) via
/// GameManager, so a later death respawns here instead of back at the level's original
/// spawn point. Fires once - re-touching an already-active checkpoint does nothing.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional - swapped in when this checkpoint activates, to show the player it's " +
        "live. Leave unassigned to just keep the current sprite.")]
    [SerializeField] private Sprite activeSprite;

    private SpriteRenderer spriteRenderer;
    private bool activated;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (other.GetComponent<PlayerMovementScript>() == null) return;

        activated = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
        }

        if (activeSprite != null)
        {
            spriteRenderer.sprite = activeSprite;
        }
    }
}
