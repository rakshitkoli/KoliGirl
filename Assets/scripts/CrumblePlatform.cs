using System.Collections;
using UnityEngine;

/// <summary>
/// A solid platform that shakes briefly once the player lands on it, then drops away
/// (collider + sprite disabled) and respawns back in place after a delay. Requires a
/// non-trigger Collider2D on this object - works with a static platform (no Rigidbody2D
/// needed), unlike MovingPlatform.
/// </summary>
public class CrumblePlatform : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.05f;
    [SerializeField] private float respawnDelay = 3f;

    private Collider2D myCollider;
    private SpriteRenderer mySprite;
    private Vector3 restPosition;
    private bool triggered;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        mySprite = GetComponent<SpriteRenderer>();
        restPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;
        if (collision.collider.GetComponent<PlayerMovementScript>() == null) return;

        bool onTop = collision.collider.bounds.min.y >= myCollider.bounds.max.y - 0.05f;
        if (!onTop) return;

        triggered = true;
        StartCoroutine(CrumbleAndRespawn());
    }

    private IEnumerator CrumbleAndRespawn()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offset = Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = restPosition + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = restPosition;
        myCollider.enabled = false;
        mySprite.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = restPosition;
        myCollider.enabled = true;
        mySprite.enabled = true;
        triggered = false;
    }
}
