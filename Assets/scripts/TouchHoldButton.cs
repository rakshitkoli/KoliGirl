using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen Left/Right/Duck button - held while pressed, released on lift, matching how a
/// physical D-pad button behaves (unlike TouchTapButton's single-press-triggers-once). Reports
/// its held state to a TouchMovementController rather than touching PlayerMovementScript
/// directly, so Left/Right/Duck can combine into one moveInput write per frame.
///
/// OnPointerExit is also wired to release - without it, dragging a finger off the button while
/// still touching the screen would leave it stuck "held" forever (PointerUp only fires back on
/// the object the press started on if the finger lifts while still over it).
/// </summary>
public class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private enum Direction { Left, Right, Down }

    [SerializeField] private TouchMovementController controller;
    [SerializeField] private Direction direction;

    public void OnPointerDown(PointerEventData eventData) => SetHeld(true);
    public void OnPointerUp(PointerEventData eventData) => SetHeld(false);
    public void OnPointerExit(PointerEventData eventData) => SetHeld(false);

    private void SetHeld(bool held)
    {
        if (controller == null) return;
        switch (direction)
        {
            case Direction.Left: controller.SetLeft(held); break;
            case Direction.Right: controller.SetRight(held); break;
            case Direction.Down: controller.SetDown(held); break;
        }
    }
}
