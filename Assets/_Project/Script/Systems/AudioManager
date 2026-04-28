using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A centralized audio manager that listens to EventBus.OnPlaySFX to play sound effects.
/// Uses a lightweight internal pool of AudioSources to avoid creating/destroying GameObjects per clip.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Pool Settings")]
    [Tooltip("Maximum number of concurrent sound effects. Beyond this, oldest sources are recycled.")]
    public int maxConcurrentSFX = 16;

    private List<AudioSource> _sourcePool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.OnPlaySFX += PlaySFX;
    }

    private void OnDisable()
    {
        EventBus.OnPlaySFX -= PlaySFX;
    }

    /// <summary>
    /// Plays a sound effect at the given world position using a pooled AudioSource.
    /// </summary>
    public void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = masterVolume * sfxVolume;
        source.spatialBlend = 1f; // Full 3D
        source.Play();
    }

    /// <summary>
    /// Plays a one-shot clip through a pooled 2D source (for UI sounds, etc.)
    /// </summary>
    public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.transform.position = transform.position;
        source.clip = clip;
        source.volume = masterVolume * sfxVolume * volumeScale;
        source.spatialBlend = 0f; // Full 2D
        source.Play();
    }

    private AudioSource GetAvailableSource()
    {
        // 1. Try to find an idle source
        foreach (var src in _sourcePool)
        {
            if (src != null && !src.isPlaying)
            {
                return src;
            }
        }

        // 2. If under the limit, create a new one
        if (_sourcePool.Count < maxConcurrentSFX)
        {
            return CreateNewSource();
        }

        // 3. Over the limit — steal the oldest (first in list)
        AudioSource oldest = _sourcePool[0];
        oldest.Stop();
        // Move to end so round-robin works
        _sourcePool.RemoveAt(0);
        _sourcePool.Add(oldest);
        return oldest;
    }

    private AudioSource CreateNewSource()
    {
        GameObject child = new GameObject($"SFX_Source_{_sourcePool.Count}");
        child.transform.SetParent(transform);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        _sourcePool.Add(src);
        return src;
    }
}
