using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private Transform goalsContainer; // Prefabları dizeceğimiz kutu (HorizontalLayoutGroup olacak)
    [SerializeField] private GoalUIView goalPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Bu fonksiyonu LevelManager çağıracak (Orkestrasyon)
    public void InitializeUI(List<LevelGoal> activeGoals, int startingMoves)
    {
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
}