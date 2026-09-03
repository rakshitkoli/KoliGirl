using UnityEngine;

/// <summary>
/// Plays one looping background music clip for the scene it lives in. Not persistent /
/// not a singleton on purpose - each scene that wants music gets its own instance with its
/// own clip, so different levels can have different tunes (see Level1.unity / Level2.unity).
/// Keeps playing through the pause menu (Time.timeScale doesn't affect audio) - that's the
/// intended behavior, not a bug.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    private void Awake()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.clip = musicClip;
        source.volume = volume;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D - background music, not positional

        if (musicClip != null) source.Play();
    }
}
