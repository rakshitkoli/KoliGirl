using UnityEngine;

/// <summary>
/// Attach to a coin GameObject (e.g. one using the SPA_Coins sprite) with a
/// Collider2D set to "Is Trigger".
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;

    // Guards against a double-count: Unity dispatches every overlapping trigger pair for a
    // physics step as a batch, so if the player ever has more than one collider (this bit us
    // for real - most levels' Koli Girl instance had a stray duplicate CapsuleCollider2D),
    // OnTriggerEnter2D fires once per collider *before* SetActive(false) below has any chance
    // to stop the second one. Checked first, same idiom as Hazard's IsDead and Checkpoint's
    // activated guard.
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerMovementScript>() == null) return;

        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectCoin(value);
        }

        gameObject.SetActive(false);
    }
}
