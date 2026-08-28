using UnityEngine;

/// <summary>
/// Attach to a coin GameObject (e.g. one using the SPA_Coins sprite) with a
/// Collider2D set to "Is Trigger".
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovementScript>() == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectCoin(value);
        }

        gameObject.SetActive(false);
    }
}
