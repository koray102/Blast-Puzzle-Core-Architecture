using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public static LevelData TargetLevelData;

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
        if(TargetLevelData != null) {currentLevelData = TargetLevelData;}
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
        
        // YENİ: Roket patlamalarını da dinle
        BoardController.Instance.Model.OnBoosterDetonated += HandleBoosterDetonated;
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

    private void HandleBoosterDetonated(Node sourceBooster, List<Node> affectedNodes)
    {
        if (_isLevelEnded) return;

        // Roketin/Bombanın yok ettiği tüm hedefleri sayaca bildir
        foreach (Node node in affectedNodes)
        {
            foreach (var goal in currentLevelData.levelGoals)
            {
                goal.UpdateGoal(node);
            }
        }

        CheckWinLoseCondition();
    }
    
    private void CheckWinLoseCondition()
    {
        // Eğer oyun zaten bittiyse (Coroutine çalışıyorsa) tekrar tetiklenmesini engelle
        if (_isLevelEnded) return;

        bool allGoalsMet = true;

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
            _isLevelEnded = true; 
            StartCoroutine(WaitAndEndLevel(true));
        }
        else if (_remainingMoves <= 0)
        {
            _isLevelEnded = true;
            StartCoroutine(WaitAndEndLevel(false));
        }
    }

    private System.Collections.IEnumerator WaitAndEndLevel(bool isWin)
    {
        // 1. SİHİRLİ BEKLEYİŞ: Tahtadaki tüm animasyonların, düşmelerin ve patlamaların bitmesini bekle
        yield return new WaitUntil(() => BoardController.Instance.State == GameState.WaitingForInput);

        // 2. OYUN HİSSİYATI (Game Feel): Her şey durduktan sonra küt diye açılmaması için ufak bir es
        yield return new WaitForSeconds(0.4f);

        if (isWin)
        {
            LevelWon();
        }
        else
        {
            LevelLost();
        }
    }

    private void LevelWon()
    {
        BoardController.Instance.SetState(GameState.GameOver);
        Debug.Log("TEBRİKLER! BÖLÜMÜ GEÇTİN!");
        GameplayUIManager.Instance.ShowWinPanel(); // Artık doğru zamanda açılacak!
    }

    private void LevelLost()
    {
        BoardController.Instance.SetState(GameState.GameOver);
        Debug.Log("HAMLEN BİTTİ! KAYBETTİN!");
        GameplayUIManager.Instance.ShowLosePanel(); // Artık doğru zamanda açılacak!
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
            BoardController.Instance.Model.OnBoosterDetonated -= HandleBoosterDetonated;
        }
    }
}