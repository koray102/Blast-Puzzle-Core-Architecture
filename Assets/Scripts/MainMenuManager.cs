using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public struct MenuPanelData
    {
        public MainMenuPanelType type;
        public GameObject panelObject;
    }

    [Header("Panel Configurations")]
    [Tooltip("Tüm menü panellerini buraya ekleyin. Aktif olan dışındakiler otomatik kapanır.")]
    [SerializeField] private List<MenuPanelData> allPanels;
    private Dictionary<MainMenuPanelType, GameObject> _panelDictionary;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button quitButton;

    [Header("Game Connection")]
    [Tooltip("Oyuna başla tuşuna basıldığında yüklenecek ilk bölüm (Level 1)")]
    [SerializeField] private LevelData firstLevel; 

    private void Awake()
    {
        InitPanelDictionary();
    }

    private void Start()
    {
        // Dinleyicileri (Eventleri) koda bağlıyoruz
        playButton.onClick.AddListener(PlayGame);
        settingsButton.onClick.AddListener(OpenSettings);
        closeSettingsButton.onClick.AddListener(CloseSettings);
        quitButton.onClick.AddListener(QuitGame);

        // Menü ilk açıldığında Ana Ekranı göster, gerisini kapat
        SwitchToPanel(MainMenuPanelType.MainScreen);
    }

    // Sözlüğü (Dictionary) başlatır
    private void InitPanelDictionary()
    {
        _panelDictionary = new Dictionary<MainMenuPanelType, GameObject>();
        foreach (var panel in allPanels)
        {
            if (panel.panelObject != null && !_panelDictionary.ContainsKey(panel.type))
            {
                _panelDictionary.Add(panel.type, panel.panelObject);
            }
        }
    }

    // --- SİHİRLİ MERKEZİ METOT ---
    public void SwitchToPanel(MainMenuPanelType targetPanelType)
    {
        foreach (var pair in _panelDictionary)
        {
            pair.Value.SetActive(pair.Key == targetPanelType);
        }
    }

    private void PlayGame()
    {
        // 1. LevelManager'a oyuna hangi bölümle başlayacağını söylüyoruz
        if (firstLevel != null)
        {
            LevelManager.TargetLevelData = firstLevel;
        }

        // 2. Gameplay sahnesini yüklüyoruz
        SceneManager.LoadScene("Game Scene"); 
    }

    private void OpenSettings()
    {
        SwitchToPanel(MainMenuPanelType.SettingsScreen);
    }

    private void CloseSettings()
    {
        SwitchToPanel(MainMenuPanelType.MainScreen);
    }

    private void QuitGame()
    {
        Debug.Log("Oyun Kapatılıyor...");
        Application.Quit(); // Not: Unity Editöründe çalışmaz, sadece Build alındığında çalışır
    }

    private void OnDestroy()
    {
        // Sahne değiştiğinde veya obje silindiğinde hafıza sızıntısını (memory leak) önle
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }
}