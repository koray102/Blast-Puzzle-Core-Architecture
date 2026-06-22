using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] private LevelData currentLevelData;
    
    private int _remainingMoves;
    private bool _isLevelEnded = false;

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
        StartLevel();
    }

    private void StartLevel()
    {
        _isLevelEnded = false;
        _remainingMoves = currentLevelData.totalMoves;

        foreach (LevelGoal goal in currentLevelData.levelGoals)
        {
            goal.Init();
        }

        // 1. Önce Tahtayı Kesin Olarak Oluştur!
        // (İleride currentLevelData içinden levelWidth ve levelHeight değerlerini de okuyabiliriz)
        BoardController.Instance.InitializeBoard(currentLevelData.boardWidth, currentLevelData.boardHeight);

        // 2. SONRA UI'I KUR (Race Condition engellendi)
        GameplayUIManager.Instance.InitializeUI(currentLevelData.levelGoals, _remainingMoves);

        // 2. Tahta %100 oluştuğuna göre artık güvenle abone olabiliriz (Race Condition çözüldü)
        BoardController.Instance.Model.OnBlocksMatched += HandleBlocksMatched;
    }

    // Modelden patlayan blokların listesi geldi
    private void HandleBlocksMatched(List<Node> matchedNodes)
    {
        if (_isLevelEnded) return;

        // 1. Patlayan her bir bloğu hedeflere bildir
        foreach (Node node in matchedNodes)
        {
            foreach (var goal in currentLevelData.levelGoals)
            {
                Debug.Log($"update goal: {node.Type}");
                goal.UpdateGoal(node.Type);
            }
        }

        // 2. Hamle sayısını düşür (Her patlatma 1 hamledir)
        _remainingMoves--;
        GameplayUIManager.Instance.UpdateMoves(_remainingMoves);
        
        // 3. Oyun bitti mi kontrol et
        CheckWinLoseCondition();
    }

    private void CheckWinLoseCondition()
    {
        bool allGoalsMet = true;

        // Tüm hedefler tamamlandı mı?
        foreach (var goal in currentLevelData.levelGoals)
        {
            if (!goal.IsMet())
            {
                allGoalsMet = false;
                break;
            }
        }

        if (allGoalsMet)
        {
            LevelWon();
        }
        else if (_remainingMoves <= 0)
        {
            LevelLost();
        }
    }

    private void LevelWon()
    {
        _isLevelEnded = true;
        Debug.Log("TEBRİKLER! BÖLÜMÜ GEÇTİN!");
        // İleride buraya UIManager.ShowWinPanel() gelecek
    }

    private void LevelLost()
    {
        _isLevelEnded = true;
        Debug.Log("HAMLEN BİTTİ! KAYBETTİN!");
        // İleride buraya UIManager.ShowLosePanel() gelecek
    }

    private void OnDestroy()
    {
        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
        }
    }
}