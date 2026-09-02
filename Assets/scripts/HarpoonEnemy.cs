using UnityEngine;

/// <summary>
/// Stationary enemy that fires a Harpoon projectile in a fixed direction on a repeating timer -
/// the game's first ranged hazard. Add a Hazard component too if contact with the launcher
/// itself should also be lethal (usually yes). Fire direction is read from this object's own
/// facing (localScale.x sign), same convention as EnemyPatrol/FlyingPatrol, so flipping the
/// prefab in the scene aims it the other way - no separate direction field to keep in sync.
/// </summary>
public class HarpoonEnemy : MonoBehaviour
{
    [SerializeField] private GameObject harpoonPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 2.5f;
    [SerializeField] private float projectileSpeed = 6f;
    [Tooltip("Delay before the first shot, so a row of harpoon turrets doesn't all fire in lockstep.")]
    [SerializeField] private float startDelay = 0f;

    private float timer;

    private void Start()
    {
        timer = -startDelay;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < fireInterval) return;
        timer = 0f;
        Fire();
    }

    private void Fire()
    {
        if (harpoonPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        float direction = Mathf.Sign(transform.localScale.x);

        var projectileObj = Instantiate(harpoonPrefab, spawnPos, Quaternion.identity);
        var projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Launch(new Vector2(direction, 0f), projectileSpeed);
        }
    }
}
