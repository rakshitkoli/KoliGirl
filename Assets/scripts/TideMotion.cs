using UnityEngine;

/// <summary>
/// Moves this object vertically in a smooth, continuous rise-and-fall between its spawn
/// position and spawnY + riseHeight, for a rising/falling tide hazard. Pair with a trigger
/// Collider2D and a Hazard component (see Hazard.cs) so contact is only lethal while the
/// tide is actually up - starts at low tide so the player gets a beat to read the timing
/// before the first rise.
///
/// A pure rise/fall on the main curve alone reads as a rigid block sliding up and down
/// (mechanical, not water-like) - the small, faster ripple layered on top breaks that up
/// into more of a "breathing" surface motion. Pair with ScrollingWaterWave on the same
/// object for the actual wave-crest animation; this script only handles the water level.
/// </summary>
public class TideMotion : MonoBehaviour
{
    [SerializeField] private float riseHeight = 1.2f;
    [SerializeField] private float period = 4f; // seconds for one full rise-and-fall cycle
    [SerializeField] private float rippleAmplitude = 0.06f;
    [SerializeField] private float rippleSpeed = 3.5f;

    private float baseY;
    private float ripplePhase;

    private void Start()
    {
        baseY = transform.position.y;
        ripplePhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float phase = (Time.time % period) / period;
        float wave = (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f; // 0..1, starts and ends at 0 (low tide)
        float ripple = Mathf.Sin(Time.time * rippleSpeed + ripplePhase) * rippleAmplitude;

        Vector3 pos = transform.position;
        pos.y = baseY + wave * riseHeight + ripple;
        transform.position = pos;
    }
}
