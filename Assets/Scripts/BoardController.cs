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
                
                if (clickedNode != null)
                {
                    // YENİ: Patlatmadan önce "Bu hamle geçerli mi?" diye sor
                    if (Model.CanMatch(clickedNode.X, clickedNode.Y))
                    {
                        SetState(GameState.Processing);
                        boardView.ResetAnimationCounter();
                        
                        // Orkestra kilitlendi
                        boardView.LockState(); 

                        // 1. Önce tıklanan bloku şişir (Pump)
                        clickedNode.PlayPumpAnimation(() =>
                        {
                            // 2. Şişme bitince patlamayı tetikle (Knockback'ler bu satırda başlayacak)
                            Model.CheckAndMatch(clickedNode.X, clickedNode.Y);
                            
                            // 3. Yerçekimini hemen çalıştırma! Geri tepmelerin görünmesi için ufak bir es ver
                            StartCoroutine(GravityDelayRoutine());
                        });
                    }
                }
            }
        }
    }

    // Geri tepme hissini (Game Feel) oyuncuya yaşatmak için kritik bekleme anı
    private System.Collections.IEnumerator GravityDelayRoutine()
    {
        yield return new WaitForSeconds(0.12f); // Patlama ile düşme arasındaki o sihirli boşluk

        Model.ApplyGravity();
        Model.FillEmptySpaces();
        
        // Orkestra kilidini aç (Knockback'ler veya düşmeler sürüyorsa View kendi içindeki sayaçla beklemeye devam eder)
        boardView.UnlockState(); 
    }
}