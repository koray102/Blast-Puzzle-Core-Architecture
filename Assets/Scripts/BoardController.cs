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

        BlockType[,] initialGrid = null;

        // Eğer manuel kurulum isteniyorsa, BoardRow'u saf 2D matrise çevir
        if (levelData.useManualSetup && levelData.startingBoard != null)
        {
            initialGrid = new BlockType[levelData.boardWidth, levelData.boardHeight];
            
            for (int x = 0; x < levelData.boardWidth; x++)
            {
                for (int y = 0; y < levelData.boardHeight; y++)
                {
                    // Unity'de inspector yukarıdan aşağı listelenir (Y ekseni ters olabilir). 
                    // Y=0'ın en alt satır olmasını sağlamak için ufak bir çeviri yapıyoruz:
                    int invertedY = (levelData.boardHeight - 1) - y;
                    initialGrid[x, y] = levelData.startingBoard[invertedY].columns[x];
                }
            }
        }

        // Modeli manuel matris ile (veya null ise rastgele) ayağa kaldır
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