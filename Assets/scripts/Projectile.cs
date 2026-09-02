using UnityEngine;

/// <summary>
/// Simple straight-line projectile (a thrown harpoon) - moves at a constant velocity, kills the
/// player on contact like Hazard.cs, and destroys itself on hitting Ground or after lifeTime, so
/// a missed shot doesn't fly forever. Fired by HarpoonEnemy.Launch().
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 4f;

    private Vector2 velocity;
    private int groundMask;

    private void Awake()
    {
        groundMask = LayerMask.GetMask("Ground");
    }

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;
        Destroy(gameObject, lifeTime);

        // Face the direction it's travelling, same flip convention (localScale.x sign) as
        // EnemyPatrol/FlyingPatrol.
        if (velocity.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerMovementScript>();
        if (player != null)
        {
            player.Die();
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & groundMask) != 0)
        {
            Destroy(gameObject);
        }
    }
}
