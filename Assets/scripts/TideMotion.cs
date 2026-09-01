using UnityEngine;

/// <summary>
/// Moves this object vertically in a smooth, continuous rise-and-fall between its spawn
/// position and spawnY + riseHeight, for a rising/falling tide hazard. Pair with a trigger
/// Collider2D and a Hazard component (see Hazard.cs) so contact is only lethal while the
/// tide is actually up - starts at low tide so the player gets a beat to read the timing
/// before the first rise.
/// </summary>
public class TideMotion : MonoBehaviour
{
    [SerializeField] private float riseHeight = 1.2f;
    [SerializeField] private float period = 4f; // seconds for one full rise-and-fall cycle

    private float baseY;

    private void Start()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        float phase = (Time.time % period) / period;
        float wave = (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f; // 0..1, starts and ends at 0 (low tide)

        Vector3 pos = transform.position;
        pos.y = baseY + wave * riseHeight;
        transform.position = pos;
    }
}
