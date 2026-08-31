using UnityEngine;

/// <summary>
/// Classic 2D parallax scrolling: this layer follows the camera's horizontal movement by a
/// fraction of the camera's own delta, so distant layers appear to drift slower than close
/// ones. Attach to each background art layer (Sky, Clouds, Layer #1-4, ...).
///
/// parallaxFactor: 0 = layer is fixed in world space, scrolls past at the same rate as normal
/// gameplay geometry (use for the closest layer). 1 = layer moves exactly with the camera, so
/// it never appears to shift on screen at all (use for the most distant layer, e.g. Sky).
/// Everything in between gives the usual "closer layers scroll faster" depth illusion.
///
/// Runs on a single orthographic camera (no separate perspective camera needed), so there is
/// no projection mismatch between this layer and anything else in the scene - unlike a
/// perspective-camera-based parallax rig, nothing here can drift out of alignment with
/// foreground objects (trees, platforms, etc).
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField] [Range(0f, 1f)] private float parallaxFactor = 0.5f;

    private Vector3 lastCamPos;

    private void Start()
    {
        if (cam == null)
        {
            GameObject mainCamGo = GameObject.Find("main camera");
            if (mainCamGo != null) cam = mainCamGo.transform;
        }
        if (cam == null)
        {
            enabled = false;
            return;
        }
        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        Vector3 delta = cam.position - lastCamPos;
        if (delta.x != 0f)
        {
            transform.position += new Vector3(delta.x * parallaxFactor, 0f, 0f);
        }
        lastCamPos = cam.position;
    }
}
