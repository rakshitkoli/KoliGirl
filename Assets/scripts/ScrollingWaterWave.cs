using UnityEngine;

/// <summary>
/// Cycles this object's SpriteRenderer through a small set of phase-shifted wave-crest
/// frames on a timer, for a cheap "flowing water" effect (a flipbook, not a shader) - the
/// same family of technique as CoinShine's pulse/sparkle. Tried scrolling the texture via
/// SpriteRenderer.material.mainTextureOffset first; Unity's default Sprite shader doesn't
/// actually respond to that at runtime (the offset value changes but nothing visibly
/// scrolls), so this swaps between a handful of pre-rendered frames instead - reliable,
/// no shader needed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollingWaterWave : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 6f;

    private SpriteRenderer spriteRenderer;
    private Vector2 tiledSize;
    private float timer;
    private int index;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Reassigning .sprite resets a Tiled SpriteRenderer's .size back to the sprite's
        // native pixel dimensions - cache the intended (already-configured) size once here
        // and re-apply it after every frame swap below, or the water would snap back to a
        // ~5-unit-wide native size every ~1/framesPerSecond seconds during play.
        tiledSize = spriteRenderer.size;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        if (timer < frameDuration) return;

        timer -= frameDuration;
        index = (index + 1) % frames.Length;
        spriteRenderer.sprite = frames[index];
        spriteRenderer.size = tiledSize;
    }
}
