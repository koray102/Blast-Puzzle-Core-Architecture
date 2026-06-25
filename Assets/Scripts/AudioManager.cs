using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource vfxSource;

    [Header("Test Audio Clips (Optional)")]
    [SerializeField] private AudioClip backgroundMusic;

    // PlayerPrefs Anahtarları
    private const string MusicVolumeKey = "MusicVolume";
    private const string MusicMutedKey = "MusicMuted";
    private const string VFXVolumeKey = "VFXVolume";
    private const string VFXMutedKey = "VFXMuted";

    // Kapsülleme (Encapsulation) Properties
    public float MusicVolume { get; private set; }
    public bool IsMusicMuted { get; private set; }
    public float VFXVolume { get; private set; }
    public bool IsVFXMuted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void Start()
    {
        // Eğer arka plan müziği atanmışsa oyun başlarken otomatik çal
        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    private void LoadSettings()
    {
        // Varsayılan değerler: Ses %100 (1f), Mute kapalı (0)
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        IsMusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        VFXVolume = PlayerPrefs.GetFloat(VFXVolumeKey, 1f);
        IsVFXMuted = PlayerPrefs.GetInt(VFXMutedKey, 0) == 1;

        UpdateAudioSources();
    }

    private void UpdateAudioSources()
    {
        // Müzik kaynağını güncelle
        musicSource.volume = IsMusicMuted ? 0f : MusicVolume;
        
        // VFX kaynağını güncelle
        vfxSource.volume = IsVFXMuted ? 0f : VFXVolume;
    }

    // --- DIŞARIYA AÇILAN KONTROL METODLARI ---

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        UpdateAudioSources();
    }

    public void ToggleMusicMute()
    {
        IsMusicMuted = !IsMusicMuted;
        PlayerPrefs.SetInt(MusicMutedKey, IsMusicMuted ? 1 : 0);
        UpdateAudioSources();
    }

    public void SetVFXVolume(float volume)
    {
        VFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VFXVolumeKey, VFXVolume);
        UpdateAudioSources();
    }

    public void ToggleVFXMute()
    {
        IsVFXMuted = !IsVFXMuted;
        PlayerPrefs.SetInt(VFXMutedKey, IsVFXMuted ? 1 : 0);
        UpdateAudioSources();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (vfxSource == null || clip == null || IsVFXMuted) return;
        vfxSource.PlayOneShot(clip, VFXVolume);
    }
}