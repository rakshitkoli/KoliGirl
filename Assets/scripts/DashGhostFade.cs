using UnityEngine;

/// <summary>
/// A single fading "afterimage" left behind during a Dash (see PlayerMovementScript.SpawnGhost).
/// Koli Girl is a composite bone rig (Body/Face/Scarf/Basket/limbs, each its own SpriteRenderer -
/// see PlayerMovementScript.bodyPartRenderers), not one sprite, so a ghost has to clone every
/// part's current world pose to actually look like her silhouette rather than a single floating
/// piece. Self-contained: builds its own child SpriteRenderers and destroys itself once faded.
/// </summary>
public class DashGhostFade : MonoBehaviour
{
    private SpriteRenderer[] parts;
    private Color startColor;
    private float fadeTime;
    private float elapsed;

    public void Init(SpriteRenderer[] sourceParts, Color tint, float fadeDuration)
    {
        parts = new SpriteRenderer[sourceParts.Length];
        for (int i = 0; i < sourceParts.Length; i++)
        {
            var src = sourceParts[i];
            if (src == null || src.sprite == null) continue;

            var partObj = new GameObject(src.gameObject.name + "_ghost");
            partObj.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            partObj.transform.localScale = src.transform.lossyScale;

            var sr = partObj.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.color = tint;
            sr.sortingLayerID = src.sortingLayerID;
            // One behind that part's usual order, so the trail never draws on top of the real
            // (currently-rendering) character.
            sr.sortingOrder = src.sortingOrder - 1;

            partObj.transform.SetParent(transform, worldPositionStays: true);
            parts[i] = sr;
        }

        startColor = tint;
        fadeTime = Mathf.Max(0.01f, fadeDuration);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / fadeTime);
        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);

        foreach (var sr in parts)
        {
            if (sr != null) sr.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
