using UnityEngine;

/// <summary>
/// Gentle vertical bob in place for a stationary jellyfish - no horizontal patrol, just a slow
/// float up/down around its spawn point. Pair with a trigger Collider2D and a Hazard component
/// (see Hazard.cs) to make contact lethal, the same wiring pattern as EnemyPatrol/FlyingPatrol.
/// </summary>
public class JellyfishFloat : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.4f;
    [SerializeField] private float bobSpeed = 1.2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, startPos.y + y, startPos.z);
    }
}
