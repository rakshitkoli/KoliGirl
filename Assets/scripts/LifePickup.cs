using UnityEngine;

/// <summary>
/// Rare +1 life pickup (e.g. one Heart_Icon per level, tucked somewhere a little out of the
/// way). Attach to a GameObject with a Collider2D set to "Is Trigger".
/// </summary>
public class LifePickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;

    // Same double-fire guard as CoinPickup - see its comment for why this is needed even
    // though SetActive(false) below looks like it should be enough on its own.
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerMovementScript>() == null) return;

        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddLife(amount);
        }

        gameObject.SetActive(false);
    }
}
