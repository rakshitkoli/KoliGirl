using UnityEngine;

/// <summary>
/// Ground patrol that moves via discrete leaping hops (parabolic arc) instead of a smooth
/// slide, for enemies whose limb animation reads as a leap/scamper rather than a walk cycle -
/// e.g. Langur Enemy. Same left/right patrol-bound convention as EnemyPatrol (flips
/// localScale.x at patrolDistance from spawn), so it's a drop-in replacement.
/// </summary>
public class MonkeyJumpPatrol : MonoBehaviour
{
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float hopDistance = 1.2f;
    [SerializeField] private float hopHeight = 0.6f;
    [SerializeField] private float hopDuration = 0.5f;
    [SerializeField] private float pauseBetweenHops = 0.25f;
    [SerializeField] private LayerMask obstacleLayer = 1 << 6; // Ground layer - statues, stones, etc.
    [SerializeField] private float obstacleCheckDistance = 0.6f;

    private Vector3 startPos;
    private Vector3 hopStartPos;
    private Vector3 hopEndPos;
    private float hopTimer;
    private float pauseTimer;
    private int direction = 1;
    private bool hopping;

    private void Start()
    {
        startPos = transform.position;
        hopStartPos = transform.position;
        BeginPause();
    }

    private void Update()
    {
        if (hopping)
        {
            hopTimer += Time.deltaTime;
            float t = Mathf.Clamp01(hopTimer / hopDuration);
            Vector3 pos = Vector3.Lerp(hopStartPos, hopEndPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = pos;

            if (t >= 1f)
            {
                hopping = false;
                BeginPause();
            }
        }
        else
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                BeginHop();
            }
        }
    }

private void BeginPause()
    {
        pauseTimer = pauseBetweenHops;

        float offset = transform.position.x - startPos.x;
        if (offset > patrolDistance) direction = -1;
        else if (offset < -patrolDistance) direction = 1;

        if (IsBlockedAhead(direction))
        {
            direction *= -1;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction < 0 ? -1f : 1f);
        transform.localScale = scale;
    }

    private bool IsBlockedAhead(int dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * dir, obstacleCheckDistance, obstacleLayer);
        return hit.collider != null;
    }

    private void BeginHop()
    {
        hopping = true;
        hopTimer = 0f;
        hopStartPos = transform.position;
        hopEndPos = hopStartPos + Vector3.right * (direction * hopDistance);
    }
}
