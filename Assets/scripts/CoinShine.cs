using UnityEngine;

/// <summary>
/// Idle "shine" effect for collectible coins: a soft brightness pulse on the coin itself
/// plus a small twinkling sparkle highlight that fades in/out and slowly spins.
/// Attach directly to a coin GameObject (reads its own SpriteRenderer). Optionally assign
/// a child SpriteRenderer (e.g. using the Sparkle sprite) to sparkleRenderer for the twinkle;
/// leave it unassigned to just get the brightness pulse.
/// </summary>
public class CoinShine : MonoBehaviour
{
    [SerializeField] private SpriteRenderer coinRenderer;
    [SerializeField] private SpriteRenderer sparkleRenderer;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseStrength = 0.22f;
    [SerializeField] private float sparkleRotateSpeed = 60f;

    private float phase;

    private void Awake()
    {
        if (coinRenderer == null) coinRenderer = GetComponent<SpriteRenderer>();
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * pulseSpeed + phase) * 0.5f + 0.5f;

        if (coinRenderer != null)
        {
            float mult = Mathf.Lerp(1f - pulseStrength, 1f, t);
            coinRenderer.color = new Color(mult, mult, mult, 1f);
        }

        if (sparkleRenderer != null)
        {
            sparkleRenderer.transform.Rotate(0f, 0f, sparkleRotateSpeed * Time.deltaTime);
            Color c = sparkleRenderer.color;
            c.a = Mathf.Clamp01(t * 1.3f - 0.15f);
            sparkleRenderer.color = c;
        }
    }
}
