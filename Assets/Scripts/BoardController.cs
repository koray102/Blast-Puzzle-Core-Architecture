using UnityEngine;


public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }

    [Header("Reference Settings")]
    [SerializeField] private BoardView boardView;
    

    // Sadece Model'i tutuyor, mantık hesaplamıyor.
    public GridModel Model { get; private set; }
    public GameState State { get; private set; } = GameState.WaitingForInput;
    public void SetState(GameState newState)
    {
        State = newState;
        
        // Oyun duraklatıldığında Unity'nin fizik ve animasyon zamanını dondur
        if (State == GameState.Paused)
        {
            Time.timeScale = 0f;
        }
        // Duraklatma bitip başka bir state'e geçildiğinde zamanı normale döndür
        else if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
    

    private void Awake()
    {
        // Temel Singleton Kurulumu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void InitializeBoard(LevelData levelData)
    {
        if (Model != null)
        {
            boardView.ClearBoard();
        }

        // DEĞİŞİKLİK: Saf renk matrisi yerine CellSetupData matrisi oluşturuyoruz
        CellSetupData[,] initialGrid = null;

        if (levelData.useManualSetup && levelData.startingBoard != null)
        {
            initialGrid = new CellSetupData[levelData.boardWidth, levelData.boardHeight];
            
            for (int x = 0; x < levelData.boardWidth; x++)
            {
                for (int y = 0; y < levelData.boardHeight; y++)
                {
                    // Level designer'ın kurduğu tüm hücre verisini (renk, engel, booster) kopyala
                    initialGrid[x, y] = levelData.startingBoard[y].columns[x];
                }
            }
        }

        // Modeli yeni veri tipiyle ayağa kaldırıyoruz
        Model = new GridModel(levelData.boardWidth, levelData.boardHeight, initialGrid);
        boardView.BuildBoard(Model);
        
        // Tahta kurulduğunda kilidi aç
        SetState(GameState.WaitingForInput);
    }


    private void Update()
    {
        if (State != GameState.WaitingForInput) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NodeView clickedNode = hit.collider.GetComponent<NodeView>();

                // 1. Önce Model'den bu koordinattaki asıl veriyi çekiyoruz (Mantık Katmanı)
                Node modelNode = Model.GetNode(clickedNode.X, clickedNode.Y);

                if (modelNode == null) return;
                
                if (clickedNode != null)
                {
                    // YENİ: Patlatmadan önce "Bu hamle geçerli mi?" diye sor
                    if (Model.CanMatch(clickedNode.X, clickedNode.Y))
                    {
                        SetState(GameState.Processing);
                        boardView.ResetAnimationCounter();
                        
                        // Orkestra kilitlendi
                        Debug.Log($"{clickedNode} blasted lock state");
                        boardView.LockState(); 

                        if (modelNode.Booster != BoosterType.None) // 2. EĞER TIKLANAN ŞEY BİR BOOSTER İSE:
                        {
                            // Şişme animasyonu BEKLEMEDEN anında patlat!
                            Model.CheckAndMatch(clickedNode.X, clickedNode.Y);
                            StartCoroutine(GravityDelayRoutine());
                        }
                        else // 3. EĞER TIKLANAN ŞEY NORMAL BİR BLOK İSE (Boş veya Kutu değilse):
                        {
                            // Görsel işi View katmanına (BoardView) devrediyoruz (Tam MVC uyumu)
                            boardView.PlayBlockClickFeedback(clickedNode, () =>
                            {
                                // View "Şişme bitti" dediğinde mantığı çalıştır
                                Model.CheckAndMatch(clickedNode.X, clickedNode.Y);
                                StartCoroutine(GravityDelayRoutine());
                            });
                        }
                    }
                }
            }
        }
    }

    // Geri tepme hissini (Game Feel) oyuncuya yaşatmak için kritik bekleme anı
    private System.Collections.IEnumerator GravityDelayRoutine()
    {
        yield return new WaitForSeconds(0.12f); // Patlama ile düşme arasındaki o sihirli boşluk

        // 2. YENİ EKLENEN SİHİR: Roket/Bomba VFX'leri veya geri tepmeler sürüyorsa, 
        // BoardView "Benim işim bitti" diyene kadar yerçekimini BEKLET!
        yield return new WaitWhile(() => boardView.IsPlayingEffects());
        
        Model.ApplyGravity();
        Model.FillEmptySpaces();
        
        // Orkestra kilidini aç (Knockback'ler veya düşmeler sürüyorsa View kendi içindeki sayaçla beklemeye devam eder)
        
        Debug.Log("Blast lock release");
        boardView.UnlockState(); 
    }

    // Görsel efekt oraya ulaştığında Modeli tetiklemek için köprü
    public void TriggerChainedBooster(int x, int y)
    {
        // CheckAndMatch yerine artık bizim özel zincirleme metodumuzu çağırıyoruz
        Model.DetonateChainedBooster(x, y); 
    }
}