using UnityEngine;

/// <summary>
/// Patrols a solid platform back and forth between its spawn point +/- patrolDistance,
/// along either the X or Y axis. Requires a Kinematic Rigidbody2D and a non-trigger
/// Collider2D on this object (moved via Rigidbody2D.MovePosition in FixedUpdate) so it
/// pushes overlapping bodies correctly instead of tunneling through them.
///
/// PlayerMovementScript overwrites its own rigidbody's horizontal velocity every Update,
/// so physics contact alone can't drag the player along - while the player is standing on
/// top, this script temporarily parents them to the platform so they ride it for free;
/// they're un-parented the moment they step off or jump away.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    private enum Axis { Horizontal, Vertical }

    [SerializeField] private Axis axis = Axis.Horizontal;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float patrolDistance = 3f;

    private Rigidbody2D myRigidbody;
    private Collider2D myCollider;
    private Vector2 startPos;
    private Vector2 axisVec;
    private int direction = 1;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        startPos = myRigidbody.position;
        axisVec = axis == Axis.Horizontal ? Vector2.right : Vector2.up;
    }

    private void FixedUpdate()
    {
        Vector2 nextPos = myRigidbody.position + axisVec * (direction * speed * Time.fixedDeltaTime);

        float offset = Vector2.Dot(nextPos - startPos, axisVec);
        if (offset > patrolDistance) direction = -1;
        else if (offset < -patrolDistance) direction = 1;

        myRigidbody.MovePosition(nextPos);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAttach(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAttach(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var player = collision.collider.GetComponent<PlayerMovementScript>();
        if (player != null && player.transform.parent == transform)
        {
            player.transform.SetParent(null);
        }
    }

    private void TryAttach(Collision2D collision)
    {
        var player = collision.collider.GetComponent<PlayerMovementScript>();
        if (player == null) return;

        // Only ride along while standing on top, not when bumping the platform from
        // below or the side.
        bool onTop = collision.collider.bounds.min.y >= myCollider.bounds.max.y - 0.05f;
        if (onTop)
        {
            player.transform.SetParent(transform);
        }
    }
}
