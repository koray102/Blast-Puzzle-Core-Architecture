using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button quitButton;

    [Header("Game Connection")]
    [Tooltip("Oyuna başla tuşuna basıldığında yüklenecek ilk bölüm (Level 1)")]
    [SerializeField] private LevelData firstLevel; 

    private void Start()
    {
        // Menü ilk açıldığında doğru panelleri göster
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        // Dinleyicileri (Eventleri) koda bağlıyoruz
        playButton.onClick.AddListener(PlayGame);
        settingsButton.onClick.AddListener(OpenSettings);
        closeSettingsButton.onClick.AddListener(CloseSettings);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void PlayGame()
    {
        // 1. LevelManager'a oyuna hangi bölümle başlayacağını söylüyoruz
        if (firstLevel != null)
        {
            LevelManager.TargetLevelData = firstLevel;
        }

        // 2. Gameplay sahnesini yüklüyoruz (İsmi senin sahnene göre değiştir)
        SceneManager.LoadScene("Game Scene"); 
    }

    private void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
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