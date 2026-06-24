using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private Transform goalsContainer; // Prefabları dizeceğimiz kutu (HorizontalLayoutGroup olacak)
    [SerializeField] private GoalUIView goalPrefab;


    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Level End Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button pauseButton;   // Sağ üstteki duraklatma butonu
    [SerializeField] private Button resumeButton;  // Menüdeki devam et butonu
    [SerializeField] private Button quitButton;    // Menüdeki çıkış butonu

    private GameState _previousState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    private void Start()
    {
        nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        retryButton.onClick.AddListener(OnRetryClicked);
        //pauseButton.onClick.AddListener(OpenPauseMenu);
        //resumeButton.onClick.AddListener(ClosePauseMenu);
        //quitButton.onClick.AddListener(QuitGame);
    }
    

    // Bu fonksiyonu LevelManager çağıracak (Orkestrasyon)
    public void InitializeUI(List<LevelGoal> activeGoals, int startingMoves)
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        // pauseMenuPanel.SetActive(false);

        UpdateMoves(startingMoves);
        SpawnGoalViews(activeGoals);
    }

    public void UpdateMoves(int currentMoves)
    {
        movesText.text = currentMoves.ToString();
    }

    private void SpawnGoalViews(List<LevelGoal> activeGoals)
    {
        // Önceki bölümden kalan eski prefablar varsa temizle
        foreach (Transform child in goalsContainer)
        {
            Destroy(child.gameObject);
        }

        // Yeni bölümün hedefleri için prefablar üret
        foreach (LevelGoal goal in activeGoals)
        {
            GoalUIView newGoalView = Instantiate(goalPrefab, goalsContainer);
            newGoalView.Init(goal);
        }
    }

    public void ShowWinPanel()
    {
        winPanel.SetActive(true);
    }

    public void ShowLosePanel()
    {
        losePanel.SetActive(true);
    }

    // Arayüzdeki "Next Level" butonuna basılınca çalışacak
    public void OnNextLevelClicked()
    {
        LevelManager.Instance.LoadNextLevel();
    }

    // Arayüzdeki "Retry" butonuna basılınca çalışacak
    public void OnRetryClicked()
    {
        LevelManager.Instance.RetryLevel();
    }

    private void OpenPauseMenu()
    {
        // Eğer oyun zaten bitmişse (GameOver) pause menüsünü açmayı engelle
        if (BoardController.Instance.State == GameState.GameOver) return;

        // Geri dönebilmek için mevcut durumu kaydet (WaitingForInput veya Processing olabilir)
        _previousState = BoardController.Instance.State;
        
        // Oyunu kitle ve zamanı durdur
        BoardController.Instance.SetState(GameState.Paused);
        
        // Paneli aktif et
        pauseMenuPanel.SetActive(true);
    }

    private void ClosePauseMenu()
    {
        // Paneli kapat
        pauseMenuPanel.SetActive(false);
        
        // Oyunu duraklatmadan önceki durumuna geri döndür (Zaman da otomatik 1'e dönecektir)
        BoardController.Instance.SetState(_previousState);
    }

    private void QuitGame()
    {
        // Zamanı normale almayı unutma, yoksa Ana Menü donuk başlar!
        Time.timeScale = 1f; 
        
        // SceneManager.LoadScene("MainMenu"); // Ana menüye dön
        Debug.Log("Ana Menüye Dönülüyor...");
    }

    private void OnDestroy()
    {
        // Kural: Kod ile abone olduğun her eventten, obje silinirken çıkış yap! (Memory leak önleme)
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
        if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }
}