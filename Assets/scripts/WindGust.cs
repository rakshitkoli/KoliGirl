using UnityEngine;

/// <summary>
/// Trigger volume that pushes the player sideways while they're inside it. Attach to a
/// (usually lightly-tinted) BoxCollider2D set to "Is Trigger". Talks to PlayerMovementScript's
/// windPush field directly (see SetWindPush) rather than touching Rigidbody2D velocity here -
/// PlayerMovementScript.Run() sets velocity.x fresh every Update() from moveInput, so anything
/// added any other way would just get overwritten the same frame.
/// </summary>
public class WindGust : MonoBehaviour
{
    [SerializeField] private float pushSpeed = 4f;
    [SerializeField] private bool pushRight = true;

    private void OnTriggerStay2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementScript>();
        if (player == null) return;
        player.SetWindPush(pushRight ? pushSpeed : -pushSpeed);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementScript>();
        if (player != null) player.SetWindPush(0f);
    }
}
