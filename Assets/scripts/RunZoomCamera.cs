using Cinemachine;
using UnityEngine;

/// <summary>
/// Smoothly zooms the Cinemachine follow camera out while the player is running, and back
/// in once they stop, giving a bit more forward visibility at speed.
/// Attach to the same GameObject as the CinemachineVirtualCamera (e.g. "Follow Camera").
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class RunZoomCamera : MonoBehaviour
{
    [SerializeField] private PlayerMovementScript player;
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float runSize = 6.2f;
    [SerializeField] private float zoomLerpSpeed = 3f;

    private CinemachineVirtualCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        float targetSize = player.IsRunning ? runSize : normalSize;

        LensSettings lens = vcam.m_Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetSize, Time.deltaTime * zoomLerpSpeed);
        vcam.m_Lens = lens;
    }
}
