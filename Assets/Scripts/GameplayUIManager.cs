using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [System.Serializable]
    public struct PanelData
    {
        public PanelType type;
        public GameObject panelObject;
    }

    [Header("Panel Configurations")]
    [Tooltip("Tüm panelleri buraya ekleyin. Aktif olan dışındakiler otomatik kapanır.")]
    [SerializeField] private List<PanelData> allPanels;
    private Dictionary<PanelType, GameObject> _panelDictionary;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private Transform goalsContainer; 
    [SerializeField] private GoalUIView goalPrefab;

    [Header("Level End Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    [Header("Menu Buttons")]
    [SerializeField] private Button pauseButton;   
    [SerializeField] private Button resumeButton;  
    [SerializeField] private Button quitButton;    

    private GameState _previousState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitPanelDictionary();
    }

    private void Start()
    {
        nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        retryButton.onClick.AddListener(OnRetryClicked);
        pauseButton.onClick.AddListener(OpenPauseMenu);
        resumeButton.onClick.AddListener(ClosePauseMenu);
        quitButton.onClick.AddListener(QuitGame);
    }

    // Sözlüğü (Dictionary) başlatır
    private void InitPanelDictionary()
    {
        _panelDictionary = new Dictionary<PanelType, GameObject>();
        foreach (var panel in allPanels)
        {
            if (panel.panelObject != null && !_panelDictionary.ContainsKey(panel.type))
            {
                _panelDictionary.Add(panel.type, panel.panelObject);
            }
        }
    }

    // --- SİHİRLİ MERKEZİ METOT ---
    // Bu metot çağrıldığında hedeflenen panel açılır, diğer TÜM paneller kapanır.
    public void SwitchToPanel(PanelType targetPanelType)
    {
        foreach (var pair in _panelDictionary)
        {
            pair.Value.SetActive(pair.Key == targetPanelType);
        }
    }

    // Bölüm başlarken LevelManager tarafından çağrılır
    public void InitializeUI(List<LevelGoal> activeGoals, int startingMoves)
    {
        UpdateMoves(startingMoves);
        SpawnGoalViews(activeGoals);
        
        // Oyun başlarken sadece HUD ekranını aç
        SwitchToPanel(PanelType.GameplayHUD);
    }

    public void UpdateMoves(int currentMoves)
    {
        if (movesText != null)
            movesText.text = currentMoves.ToString();
    }

    private void SpawnGoalViews(List<LevelGoal> activeGoals)
    {
        foreach (Transform child in goalsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (LevelGoal goal in activeGoals)
        {
            GoalUIView newGoalView = Instantiate(goalPrefab, goalsContainer);
            newGoalView.Init(goal);
        }
    }

    // Oyun sonu ekranları
    public void ShowWinPanel()
    {
        SwitchToPanel(PanelType.WinWindow);
    }

    public void ShowLosePanel()
    {
        SwitchToPanel(PanelType.LoseWindow);
    }

    // Arayüz buton fonksiyonları
    public void OnNextLevelClicked()
    {
        LevelManager.Instance.LoadNextLevel();
    }

    public void OnRetryClicked()
    {
        LevelManager.Instance.RetryLevel();
    }

    private void OpenPauseMenu()
    {
        // Oyun bittiyse duraklatmayı engelle
        if (BoardController.Instance.State == GameState.GameOver) return;

        _previousState = BoardController.Instance.State;
        BoardController.Instance.SetState(GameState.Paused);
        
        SwitchToPanel(PanelType.PauseMenu);
    }

    private void ClosePauseMenu()
    {
        SwitchToPanel(PanelType.GameplayHUD);
        BoardController.Instance.SetState(_previousState);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f; 
        Debug.Log("Ana Menüye Dönülüyor...");
        SceneManager.LoadScene("Main Menu Scene"); 
    }

    private void OnDestroy()
    {
        // Memory leak önleme
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
        if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }
}