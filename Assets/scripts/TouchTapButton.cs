using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen Jump/Dash button - fires once per tap (on press, matching value.isPressed in the
/// keyboard/gamepad path) rather than being held like TouchHoldButton's D-pad buttons.
///
/// Looks the player up itself (FindFirstObjectByType) rather than taking a serialized
/// PlayerMovementScript reference - a reference like that can only point at another object in
/// the same scene, which would mean re-wiring it by hand in every one of the 20 level scenes
/// this prefab gets placed into. This way the whole touch-controls prefab is drop-in identical
/// everywhere.
/// </summary>
public class TouchTapButton : MonoBehaviour, IPointerDownHandler
{
    private enum Action { Jump, Dash }

    [SerializeField] private Action action;

    public void OnPointerDown(PointerEventData eventData)
    {
        var player = PlayerMovementScript.Instance;
        if (player == null) return;

        if (action == Action.Jump) player.TouchJump();
        else player.TouchDash();
    }
}
