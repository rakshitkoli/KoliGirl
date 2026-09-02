using UnityEngine;

/// <summary>
/// Makes an existing hazard (Fire, etc.) lethal only in bursts instead of constantly - safe
/// during a "lull" window, dangerous during a "flare" window, repeating. Pair with a Hazard
/// component on the same object (or a child) and a trigger Collider2D; this script only
/// toggles that collider on/off, so Hazard's own OnTriggerEnter2D naturally stops firing
/// while off. A dimmed tint during the lull (vs full brightness during the flare) is the
/// player's visual read on which phase it's in - timed avoidance instead of purely spatial.
/// </summary>
public class PulseHazard : MonoBehaviour
{
    [SerializeField] private float lullDuration = 1.6f;
    [SerializeField] private float flareDuration = 1.1f;
    [SerializeField] private Collider2D hazardCollider;
    [SerializeField] private SpriteRenderer[] tintTargets;
    [SerializeField] [Range(0f, 1f)] private float lullBrightness = 0.45f;

    [Tooltip("For a particle-based hazard (Fire): the flame/spark systems to stop during the " +
        "lull and play again during the flare, so the VFX itself reads as \"off\" - a dimmed " +
        "sprite alone doesn't sell that for something that's normally all particles.")]
    [SerializeField] private ParticleSystem[] particleTargets;

    [SerializeField] private UnityEngine.Rendering.Universal.Light2D[] lightTargets;
    [SerializeField] private float lullLightIntensity = 0.15f;
    private float[] baseLightIntensities;

    private float timer;
    private bool flaring;

    private void Start()
    {
        if (hazardCollider == null) hazardCollider = GetComponentInChildren<Collider2D>();
        if (tintTargets == null || tintTargets.Length == 0)
        {
            tintTargets = GetComponentsInChildren<SpriteRenderer>();
        }
        if (particleTargets == null || particleTargets.Length == 0)
        {
            particleTargets = GetComponentsInChildren<ParticleSystem>();
        }
        if (lightTargets == null || lightTargets.Length == 0)
        {
            lightTargets = GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>();
        }
        baseLightIntensities = new float[lightTargets.Length];
        for (int i = 0; i < lightTargets.Length; i++)
        {
            baseLightIntensities[i] = lightTargets[i] != null ? lightTargets[i].intensity : 0f;
        }

        // Start mid-lull so the player gets a beat to read the pattern before the first flare.
        timer = lullDuration * 0.5f;
        SetFlaring(false);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        SetFlaring(!flaring);
        timer = flaring ? flareDuration : lullDuration;
    }

    private void SetFlaring(bool value)
    {
        flaring = value;
        if (hazardCollider != null) hazardCollider.enabled = value;

        float b = value ? 1f : lullBrightness;
        foreach (var sr in tintTargets)
        {
            if (sr == null) continue;
            sr.color = new Color(b, b, b, 1f);
        }

        foreach (var ps in particleTargets)
        {
            if (ps == null) continue;
            if (value) ps.Play();
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        for (int i = 0; i < lightTargets.Length; i++)
        {
            if (lightTargets[i] == null) continue;
            lightTargets[i].intensity = value ? baseLightIntensities[i] : lullLightIntensity;
        }
    }
}
