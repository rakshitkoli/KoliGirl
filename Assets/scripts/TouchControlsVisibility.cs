using UnityEngine;

/// <summary>
/// Shows the on-screen touch controls only on an actual touch-capable device, so keyboard/
/// gamepad play (PC, Editor testing) isn't cluttered with buttons nobody's tapping. Attach to
/// the root of the touch controls canvas/panel.
/// </summary>
public class TouchControlsVisibility : MonoBehaviour
{
    [Tooltip("Force the controls on regardless of platform - handy for testing the layout in " +
        "the Editor Game view without a real touch device.")]
    [SerializeField] private bool forceVisibleInEditor = false;

    private void Awake()
    {
        bool shouldShow = Application.isMobilePlatform || Input.touchSupported;
#if UNITY_EDITOR
        shouldShow = forceVisibleInEditor;
#endif
        gameObject.SetActive(shouldShow);
    }
}
