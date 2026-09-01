using UnityEngine;

/// <summary>
/// Patrols a solid platform back and forth between its spawn point +/- patrolDistance,
/// along either the X or Y axis. Requires a Kinematic Rigidbody2D and a non-trigger
/// Collider2D on this object (moved via Rigidbody2D.MovePosition in FixedUpdate) so it
/// pushes overlapping bodies correctly instead of tunneling through them.
///
/// Carrying the player: parenting the player's Transform under a moving platform does NOT
/// work for this - Unity's physics drives a dynamic Rigidbody2D's position from its own
/// velocity every step, completely independent of the parent Transform's motion, so a
/// parented player still wouldn't move with the platform. (Parenting also has a nasty side
/// effect here: PlayerMovementScript.FlipSprite() overwrites the player's *local* scale
/// every frame, which - once local space is relative to this platform's own scale instead
/// of world space - visibly shrinks the player.) So instead, FixedUpdate computes this
/// step's movement delta, and OnCollisionStay2D (Unity's own verified contact detection -
/// far more reliable than a hand-rolled overlap check, which lost tracking after a couple
/// dozen physics steps in testing here) adds that same delta directly to the rigidbody
/// position of anything resting on top. Player input still drives their own velocity on
/// top of that, exactly like standing on any solid ground.
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
    private Vector2 pendingDelta;
    private bool carriedThisStep;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // Zero friction so the physics engine's own contact resolution doesn't ALSO impart
        // horizontal velocity to a resting rider on top of this platform's motion - that
        // velocity would accumulate step over step (confirmed via manual physics-step
        // testing: the rider drifted steadily ahead of the platform, never catching up)
        // since it isn't reliably cancelled until the player's own script resets its
        // velocity, which only happens once per rendered frame while many physics steps
        // can run in between. The explicit delta-carry above is the only intended transfer.
        myCollider.sharedMaterial = new PhysicsMaterial2D("MovingPlatformNoFriction") { friction = 0f };
    }

    private void Start()
    {
        startPos = myRigidbody.position;
        axisVec = axis == Axis.Horizontal ? Vector2.right : Vector2.up;
    }

    private void FixedUpdate()
    {
        Vector2 prevPos = myRigidbody.position;
        Vector2 nextPos = prevPos + axisVec * (direction * speed * Time.fixedDeltaTime);

        float offset = Vector2.Dot(nextPos - startPos, axisVec);
        if (offset > patrolDistance) direction = -1;
        else if (offset < -patrolDistance) direction = 1;

        pendingDelta = nextPos - prevPos;
        carriedThisStep = false;
        myRigidbody.MovePosition(nextPos);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // OnCollisionStay2D fires once per contact point in the manifold (commonly 2+ for
        // a box-vs-capsule contact), not once per physics step - without this guard the
        // delta gets applied multiple times per step and the rider drifts ahead.
        if (carriedThisStep) return;

        var player = collision.collider.GetComponent<PlayerMovementScript>();
        if (player == null) return;

        // Only carry while genuinely resting on top, not when bumping the platform from
        // below or the side.
        bool onTop = collision.collider.bounds.min.y >= myCollider.bounds.max.y - 0.05f;
        if (!onTop) return;

        carriedThisStep = true;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position += pendingDelta;
        }
        else
        {
            player.transform.position += (Vector3)pendingDelta;
        }
    }
}
