using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockFactory blockFactory;
    [SerializeField] private ObjectPool _pool; // Patlatma sonrası havuza göndermek için
    
    [Header("Layout Settings")]
    [SerializeField] private float spacing = 1.05f;
    
    private NodeView[,] _viewGrid;

    public void BuildBoard(GridModel model)
    {
        _viewGrid = new NodeView[model.Width, model.Height];

        // 1. Eventlere Abone Oluyoruz
        model.OnBlocksMatched += HandleBlocksMatched;
        model.OnBlocksFell += HandleBlocksFell;
        model.OnNewBlocksSpawned += HandleNewBlocksSpawned;
        model.OnNodesUpdated += HandleNodesUpdated;

        // 2. Tahtayı İlk Kez Diziyoruz
        for (int x = 0; x < model.Width; x++)
        {
            for (int y = 0; y < model.Height; y++)
            {
                Node node = model.GetNode(x, y);
                
                if (node != null)
                {
                    // Pozisyonu ortak metodumuzla hesaplıyoruz
                    Vector3 worldPosition = CalculateWorldPosition(x, y);
                    
                    NodeView spawnedBlock = blockFactory.SpawnBlock(node, worldPosition);
                    _viewGrid[x, y] = spawnedBlock;
                }
            }
        }
    }


    public void ClearBoard()
    {
        if (_viewGrid == null) return;

        // 1. Matristeki tüm objeleri bul ve havuza geri yolla
        for (int x = 0; x < _viewGrid.GetLength(0); x++)
        {
            for (int y = 0; y < _viewGrid.GetLength(1); y++)
            {
                NodeView node = _viewGrid[x, y];
                if (node != null)
                {
                    _pool.ReturnNode(node); // Objeyi gizler ve havuza ekler
                    _viewGrid[x, y] = null;
                }
            }
        }

        // 2. Çok Kritik: Eski modelin eventlerinden çıkış yap!
        // (Eğer bunu yapmazsan yeni levelda bloklar ikişer kere düşer)
        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
            BoardController.Instance.Model.OnBlocksFell -= HandleBlocksFell;
            BoardController.Instance.Model.OnNewBlocksSpawned -= HandleNewBlocksSpawned;
            BoardController.Instance.Model.OnNodesUpdated -= HandleNodesUpdated;
        }
    }


    private Vector3 CalculateWorldPosition(int x, int y)
    {
        // Model'in boyutlarını Controller üzerinden alıyoruz
        int width = BoardController.Instance.Model.Width;
        int height = BoardController.Instance.Model.Height;

        Vector3 centerOffset = new Vector3(
            -(width - 1) * spacing / 2f,
            -(height - 1) * spacing / 2f,
            0
        );

        return new Vector3(x * spacing, y * spacing, 0) + centerOffset;
    }

    // --- EVENT DİNLEYİCİLERİ (OBSERVERS) ---

    private void HandleBlocksMatched(List<Node> matchedNodes)
    {
        foreach (Node node in matchedNodes)
        {
            NodeView viewToHide = _viewGrid[node.X, node.Y];
            
            if (viewToHide != null)
            {
                _pool.ReturnNode(viewToHide); // Görseli kapat ve havuza yolla
                _viewGrid[node.X, node.Y] = null; // Matristen sil
            }
        }
    }

    private void HandleBlocksFell(List<BlockMoveData> moveDataList)
    {
        foreach (var moveData in moveDataList)
        {
            NodeView blockToMove = _viewGrid[moveData.FromX, moveData.FromY];
            if (blockToMove == null) continue;

            Vector3 targetPos = CalculateWorldPosition(moveData.ToX, moveData.ToY);

            // Matrisi Güncelle
            _viewGrid[moveData.FromX, moveData.FromY] = null;
            _viewGrid[moveData.ToX, moveData.ToY] = blockToMove;

            // X ve Y etiketlerini güncelle ve Hareketi Tetikle
            blockToMove.UpdateCoordinates(moveData.ToX, moveData.ToY);
            blockToMove.MoveTo(targetPos);
        }
    }

    private void HandleNewBlocksSpawned(List<Node> newNodes)
    {
        // Tahtanın yüksekliğini alıyoruz ki en tepe noktasını bulabilelim
        int height = BoardController.Instance.Model.Height;

        foreach (var node in newNodes)
        {
            Vector3 targetPos = CalculateWorldPosition(node.X, node.Y);
            
            // Yeni bloğun doğuş noktası: Aynı X hizasında, ama tahtanın 1-2 birim üstünde (Height + 1)
            Vector3 spawnPos = CalculateWorldPosition(node.X, height + 1); 

            // Fabrikadan üret
            NodeView newBlock = blockFactory.SpawnBlock(node, spawnPos);
            _viewGrid[node.X, node.Y] = newBlock;

            // Yukarıdan asıl hedefine doğru düşme hareketini tetikle
            newBlock.MoveTo(targetPos);
        }
    }

    private void HandleNodesUpdated(List<Node> updatedNodes)
    {
        foreach (Node node in updatedNodes)
        {
            NodeView viewToUpdate = _viewGrid[node.X, node.Y];
            if (viewToUpdate != null)
            {
                // Fabrikadan bu hücrenin yeni haline göre rengini tazelemesini iste
                blockFactory.UpdateBlockVisual(viewToUpdate, node);
            }
        }
    }

    // Memory Leak (Bellek sızıntısı) olmaması için oyun bitince abonelikleri siliyoruz
    private void OnDestroy()
    {
        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
            BoardController.Instance.Model.OnBlocksFell -= HandleBlocksFell;
            BoardController.Instance.Model.OnNewBlocksSpawned -= HandleNewBlocksSpawned;
            BoardController.Instance.Model.OnNodesUpdated -= HandleNodesUpdated;
        }
    }
}