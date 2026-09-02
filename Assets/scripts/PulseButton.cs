using UnityEngine;

/// <summary>
/// Idle "breathing" scale pulse for a call-to-action button (the Play button) - draws the eye
/// without needing a full Animator. Uses unscaled time so it keeps pulsing even if something
/// pauses Time.timeScale on a menu screen. Call PlayPressBounce() from the Button's OnClick
/// (in addition to whatever actually navigates) for a quick tactile squash on press.
/// </summary>
public class PulseButton : MonoBehaviour
{
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pressScale = 0.85f;
    [SerializeField] private float pressDuration = 0.15f;

    private Vector3 baseScale;
    private bool pressed;
    private float pressTimer;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (pressed)
        {
            pressTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(pressTimer / pressDuration);
            transform.localScale = Vector3.Lerp(baseScale * pressScale, baseScale, t);
            if (t >= 1f) pressed = false;
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * pulse;
    }

    public void PlayPressBounce()
    {
        pressed = true;
        pressTimer = 0f;
    }
}
