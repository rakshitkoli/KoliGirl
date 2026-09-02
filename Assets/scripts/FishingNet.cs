using UnityEngine;

/// <summary>
/// Contact hazard that doesn't kill - snags the player and cuts their movement to a crawl for a
/// few seconds (see PlayerMovementScript.ApplySnare/IsSnared), instead of instant death like
/// Hazard.cs. Attach to a net sprite with a trigger Collider2D. Re-triggering an already-snared
/// player refreshes the timer rather than stacking, so lingering in the net doesn't extend the
/// penalty past snareDuration.
/// </summary>
public class FishingNet : MonoBehaviour
{
    [SerializeField] private float snareDuration = 1.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementScript>();
        if (player != null)
        {
            player.ApplySnare(snareDuration);
        }
    }
}
