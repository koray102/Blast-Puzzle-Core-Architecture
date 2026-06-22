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


    [Header("End Game Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

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
    }
    

    // Bu fonksiyonu LevelManager çağıracak (Orkestrasyon)
    public void InitializeUI(List<LevelGoal> activeGoals, int startingMoves)
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);

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

    private void OnDestroy()
    {
        // Kural: Kod ile abone olduğun her eventten, obje silinirken çıkış yap! (Memory leak önleme)
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
    }
}