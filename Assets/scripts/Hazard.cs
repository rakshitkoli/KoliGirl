using UnityEngine;

/// <summary>
/// Generic instant-death hazard. Attach to Fire, Spikes, or any future obstacle,
/// with a Collider2D on that object (or a child) set to "Is Trigger".
/// </summary>
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementScript>();
        if (player != null)
        {
            player.Die();
        }
    }
}
