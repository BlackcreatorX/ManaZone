using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates and manages persistent music and sound effect sources.</summary>
public sealed class AudioManager : MonoBehaviour
{
    [System.Serializable]
    private struct SoundEffect
    {
        public string id;
        public AudioClip clip;
    }

    [Header("Audio clips")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private SoundEffect[] soundEffects;

    private static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
                Create();
            return instance;
        }
    }

    /// <summary>Creates the persistent manager if it does not already exist.</summary>
    public static AudioManager Create()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<AudioManager>();
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("AudioManager");
        return managerObject.AddComponent<AudioManager>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAtStartup()
    {
        Create();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSingleAudioListener();
        SceneManager.sceneLoaded += OnSceneLoaded;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        PlayMusic(musicClip);
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleAudioListener();
    }

    private void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        Camera preferredCamera = Camera.main;
        if (preferredCamera == null)
            preferredCamera = FindFirstObjectByType<Camera>();

        AudioListener primary = preferredCamera != null
            ? preferredCamera.GetComponent<AudioListener>()
            : null;

        if (primary == null && listeners.Length > 0)
            primary = listeners[0];

        if (primary == null)
        {
            GameObject listenerObject = preferredCamera != null ? preferredCamera.gameObject : gameObject;
            primary = listenerObject.AddComponent<AudioListener>();
        }

        primary.enabled = true;
        foreach (AudioListener listener in listeners)
        {
            if (listener != null && listener != primary)
                listener.enabled = false;
        }
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlaySfx(string id, float volume = 1f)
    {
        if (string.IsNullOrWhiteSpace(id) || soundEffects == null)
            return;

        foreach (SoundEffect effect in soundEffects)
        {
            if (string.Equals(effect.id, id, System.StringComparison.OrdinalIgnoreCase))
            {
                PlaySfx(effect.clip, volume);
                return;
            }
        }

        Debug.LogWarning($"No hay un efecto de sonido registrado con el nombre '{id}'.", this);
    }

    public void PlayMusic()
    {
        PlayMusic(musicClip);
    }

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
        musicSource.clip = null;
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);
    }
}
