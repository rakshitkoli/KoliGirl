using UnityEngine;

/// <summary>
/// Horizontal patrol combined with a periodic vertical dive/swoop, for a flying enemy
/// like a seagull. Pair with a trigger Collider2D and a Hazard component (see Hazard.cs)
/// to make contact lethal, the same wiring pattern as EnemyPatrol.
/// </summary>
public class FlyingPatrol : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float diveDepth = 1.5f;
    [SerializeField] private float diveInterval = 3f; // seconds per full dive-and-recover cycle

    private Vector3 startPos;
    private int direction = 1;
    private float diveTimer;

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

        diveTimer += Time.deltaTime;
        float divePhase = (diveTimer % diveInterval) / diveInterval; // 0..1, repeating
        float dive = Mathf.Max(0f, -Mathf.Sin(divePhase * Mathf.PI * 2f)); // dips down, never rises above patrol height

        Vector3 pos = transform.position;
        pos.y = startPos.y - dive * diveDepth;
        transform.position = pos;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction < 0 ? -1f : 1f);
        transform.localScale = scale;
    }
}
