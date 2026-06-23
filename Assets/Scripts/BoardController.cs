using UnityEngine;

public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }


    [Header("Reference Settings")]
    [SerializeField] private BoardView boardView;
    

    // Sadece Model'i tutuyor, mantık hesaplamıyor.
    public GridModel Model { get; private set; }

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
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NodeView clickedNode = hit.collider.GetComponent<NodeView>();
                
                if (clickedNode != null)
                {
                    // Patlatmayı dene. Eğer başarılıysa (true dönerse) yerçekimini ve yeni blokları tetikle!
                    bool isMatched = Model.CheckAndMatch(clickedNode.X, clickedNode.Y);
                    
                    if (isMatched)
                    {
                        Model.ApplyGravity();
                        Model.FillEmptySpaces();
                    }
                }
            }
        }
    }
}