using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Smoothly zooms the follow camera out while the player is running (and back in once they
/// stop), and keeps the camera's view inside the level's confiner bounds.
/// Attach to the same GameObject as the CinemachineCamera (e.g. "Follow Camera").
///
/// NOTE: this project is on Cinemachine 3.x, and several of its runtime mechanisms silently
/// don't take effect here - confirmed empirically for two separate features:
///   - CinemachineVirtualCamera.m_Lens / CinemachineCamera.Lens: an external write reverts
///     to the configured default within one frame, with nothing else touching it.
///   - CinemachineConfiner2D: even after baking its bounding shape (BakeBoundingShape),
///     the live camera was still observed outside the confiner polygon's bounds.
/// Rather than keep fighting CM3 internals neither of us can fully see into, this script
/// drives the real Camera.orthographicSize and clamps Camera.transform.position directly,
/// timed via DefaultExecutionOrder to run after CinemachineBrain's LateUpdate has already
/// applied its own computed values - so this script's values are what actually land on
/// screen, regardless of what's happening inside Cinemachine.
/// </summary>
[DefaultExecutionOrder(1000)]
public class RunZoomCamera : MonoBehaviour
{
    [SerializeField] private PlayerMovementScript player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float runSize = 6.2f;
    [SerializeField] private float zoomLerpSpeed = 3f;

    [Tooltip("The level's camera-confiner polygon. The camera's view (accounting for its " +
        "current orthographic size/aspect) is clamped to stay inside these world-space bounds.")]
    [SerializeField] private PolygonCollider2D confinerBounds;

    private float currentSize;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        currentSize = targetCamera != null ? targetCamera.orthographicSize : normalSize;
    }

    private void LateUpdate()
    {
        if (player == null || targetCamera == null) return;

        float targetSize = player.IsRunning ? runSize : normalSize;
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * zoomLerpSpeed);
        targetCamera.orthographicSize = currentSize;

        if (confinerBounds != null)
        {
            Bounds b = confinerBounds.bounds;
            float halfHeight = currentSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            Vector3 pos = targetCamera.transform.position;
            float minX = b.min.x + halfWidth;
            float maxX = b.max.x - halfWidth;
            float minY = b.min.y + halfHeight;
            float maxY = b.max.y - halfHeight;

            pos.x = (minX <= maxX) ? Mathf.Clamp(pos.x, minX, maxX) : b.center.x;
            pos.y = (minY <= maxY) ? Mathf.Clamp(pos.y, minY, maxY) : b.center.y;
            targetCamera.transform.position = pos;
        }
    }
}
