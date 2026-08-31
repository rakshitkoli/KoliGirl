using UnityEngine;

/// <summary>
/// Simple back-and-forth ground patrol for a 2D enemy. Moves left/right between the
/// spawn point +/- patrolDistance, flipping to face its direction of travel. Pair with a
/// trigger Collider2D and a Hazard component (see Hazard.cs) to make contact lethal - this
/// script only handles movement, not the kill-on-touch behavior.
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float patrolDistance = 3f;

    private Vector3 startPos;
    private int direction = 1;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position += Vector3.right * (direction * speed * Time.deltaTime);

        float offset = transform.position.x - startPos.x;
        if (offset > patrolDistance)
        {
            direction = -1;
        }
        else if (offset < -patrolDistance)
        {
            direction = 1;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction < 0 ? -1f : 1f);
        transform.localScale = scale;
    }
}
