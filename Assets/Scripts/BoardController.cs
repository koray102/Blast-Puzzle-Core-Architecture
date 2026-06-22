using UnityEngine;

public class BoardController : MonoBehaviour
{
    public static BoardController Instance { get; private set; }


    [Header("Reference Settings")]
    [SerializeField] private BoardView boardView;
    

    [Header("Board Settings")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 10;

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


    public void InitializeBoard(int width, int height)
    {
        Model = new GridModel(width, height);
        Debug.Log($"Model oluşturuldu: {width}x{height}");
        
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