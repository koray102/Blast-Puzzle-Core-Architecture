using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] internal LevelData currentLevelData;
    
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

        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
        }

        foreach (LevelGoal goal in currentLevelData.levelGoals)
        {
            goal.Init();
        }

        // 1. Önce Tahtayı Kesin Olarak Oluştur!
        BoardController.Instance.InitializeBoard(currentLevelData);

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
                goal.UpdateGoal(node);
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
        GameplayUIManager.Instance.ShowWinPanel();
    }

    private void LevelLost()
    {
        _isLevelEnded = true;
        Debug.Log("HAMLEN BİTTİ! KAYBETTİN!");
        GameplayUIManager.Instance.ShowLosePanel();
    }

    public void RetryLevel()
    {
        // currentLevelData zaten aynı kalacak, sadece tahtayı ve UI'ı baştan kur
        StartLevel();
    }

    public void LoadNextLevel()
    {
        if (currentLevelData.nextLevel != null)
        {
            // Veriyi bir sonraki bölüm ile değiştir ve baştan kur
            currentLevelData = currentLevelData.nextLevel;
            StartLevel();
        }
        else
        {
            Debug.Log("TEBRİKLER! OYUNDAKİ TÜM BÖLÜMLER BİTTİ!");
            // Tüm oyun bittiyse Ana Menüye dönebilirsin
        }
    }

    private void OnDestroy()
    {
        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
        }
    }
}