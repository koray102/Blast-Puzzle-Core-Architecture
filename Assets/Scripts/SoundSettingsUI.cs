using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Music UI Elements")]
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private TextMeshProUGUI musicButtonText; // ON/OFF yazısı için (Opsiyonel)
    [SerializeField] private Slider musicVolumeSlider;

    [Header("VFX UI Elements")]
    [SerializeField] private Button SFXToggleButton;
    [SerializeField] private TextMeshProUGUI SFXButtonText;   // ON/OFF yazısı için (Opsiyonel)
    [SerializeField] private Slider SFXVolumeSlider;

    private void Start()
    {
        // Güvenlik Kontrolü
        if (AudioManager.Instance == null)
        {
            Debug.LogError("[SoundSettingsUI] Sahnede AudioManager bulunamadı!");
            return;
        }

        InitUIValues();
        BindEvents();
    }

    private void InitUIValues()
    {
        // UI elemanlarını AudioManager'daki mevcut verilerle eşitliyoruz
        musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
        SFXVolumeSlider.value = AudioManager.Instance.VFXVolume;

        UpdateVisuals();
    }

    private void BindEvents()
    {
        // INSPECTOR OLMAZSIZIN TAMAMEN KODLA EVENT BINDING (Dinamik Dinleyiciler)
        
        // Müzik Kontrolleri
        musicToggleButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.ToggleMusicMute();
            UpdateVisuals();
        });

        musicVolumeSlider.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance.SetMusicVolume(value);
        });

        // SFX Kontrolleri
        SFXToggleButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.ToggleVFXMute();
            UpdateVisuals();
        });

        SFXVolumeSlider.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance.SetVFXVolume(value);
        });
    }

    private void UpdateVisuals()
    {
        // Butonların üzerindeki metinleri veya renkleri güncelleyen kısım
        if (musicButtonText != null)
            musicButtonText.text = AudioManager.Instance.IsMusicMuted ? "MUTED" : "ON";

        if (SFXButtonText != null)
            SFXButtonText.text = AudioManager.Instance.IsVFXMuted ? "MUTED" : "ON";
            
        // İstersen buralarda buton rengini de değiştirebilirsin:
        // musicToggleButton.image.color = AudioManager.Instance.IsMusicMuted ? Color.gray : Color.white;
    }

    private void OnDestroy()
    {
        // Hafıza sızıntısını (Memory Leak) önlemek için abonelikleri temizliyoruz
        if (musicToggleButton != null) musicToggleButton.onClick.RemoveAllListeners();
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (SFXToggleButton != null) SFXToggleButton.onClick.RemoveAllListeners();
        if (SFXVolumeSlider != null) SFXVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}