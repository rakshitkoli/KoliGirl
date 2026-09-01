using UnityEngine;

/// <summary>
/// Flaps this wing by oscillating its local Z rotation around a base angle, the same
/// simple sin-wave-driven style as CoinShine. Attach directly to a wing child transform
/// whose sprite pivot is set at the shoulder, so it hinges naturally.
///
/// For a left/right pair, mirror the right wing via localScale.x = -1 and give both wings
/// the same baseAngle/flapAmplitude/phase - the mirroring alone makes the flap read as a
/// symmetric pair, no sign-flipping needed here.
/// </summary>
public class WingFlap : MonoBehaviour
{
    [SerializeField] private float baseAngle = 0f;
    [SerializeField] private float flapAmplitude = 24f;
    [SerializeField] private float flapSpeed = 7f;
    [SerializeField] private float phase = 0f;

    private void Update()
    {
        float angle = baseAngle + Mathf.Sin(Time.time * flapSpeed + phase) * flapAmplitude;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
