using UnityEngine;

/// <summary>
/// Adds a bouncy vertical bob and squash-stretch to a single-sprite enemy that has no frame
/// animation, so it doesn't look like it's sliding around - e.g. Langur Enemy. Only touches
/// position.y and localScale.y, so it composes safely alongside EnemyPatrol/FlyingPatrol (which
/// only touch position.x and localScale.x's sign) on the same GameObject.
/// </summary>
public class HopBob : MonoBehaviour
{
    [SerializeField] private float hopHeight = 0.12f;
    [SerializeField] private float hopSpeed = 6f;
    [SerializeField] private float squashAmount = 0.08f;

    private float baseY;
    private float baseScaleY;

    private void Start()
    {
        baseY = transform.position.y;
        baseScaleY = transform.localScale.y;
    }

    private void Update()
    {
        float phase = Mathf.Sin(Time.time * hopSpeed);
        float hop = Mathf.Abs(phase) * hopHeight;

        Vector3 pos = transform.position;
        pos.y = baseY + hop;
        transform.position = pos;

        Vector3 scale = transform.localScale;
        scale.y = baseScaleY * (1f - squashAmount * Mathf.Abs(phase) + squashAmount * 0.5f);
        transform.localScale = scale;
    }
}
