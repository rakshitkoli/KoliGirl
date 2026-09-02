using UnityEngine;

/// <summary>
/// Aggregates the held/released state of the Left/Right/Duck touch buttons (see
/// TouchHoldButton) into a single moveInput write per frame - three independent buttons each
/// writing PlayerMovementScript.moveInput directly would fight each other and lose whichever
/// wrote first that frame. This is the one place that actually sets moveInput while touch
/// controls are in play.
///
/// Only active on a touch device (see TouchControlsVisibility), so it never fights keyboard
/// input during Editor/PC testing.
///
/// Finds the player via PlayerMovementScript.Instance rather than a serialized reference - see
/// TouchTapButton's comment for why: a scene-object reference would need re-wiring by hand in
/// every one of the 20 levels this prefab drops into.
/// </summary>
public class TouchMovementController : MonoBehaviour
{
    private bool leftHeld;
    private bool rightHeld;
    private bool downHeld;

    public void SetLeft(bool held) => leftHeld = held;
    public void SetRight(bool held) => rightHeld = held;
    public void SetDown(bool held) => downHeld = held;

    private void Update()
    {
        var player = PlayerMovementScript.Instance;
        if (player == null) return;

        float x = (leftHeld ? -1f : 0f) + (rightHeld ? 1f : 0f);
        float y = downHeld ? -1f : 0f;
        player.moveInput = new Vector2(x, y);
    }
}
