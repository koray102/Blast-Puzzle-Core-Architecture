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
                    
                    NodeView spawnedBlock = blockFactory.SpawnBlock(node.Type, worldPosition, x, y);
                    _viewGrid[x, y] = spawnedBlock;
                }
            }
        }
    }

    // ---> İŞTE EKSİK OLAN ORTAK METODUMUZ <---
    // Her defasında spaciong offset matematiğini yazmamak için bir fonksiyona çıkardık.
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
        foreach (var node in newNodes)
        {
            Vector3 targetPos = CalculateWorldPosition(node.X, node.Y);
            
            // Ekranın üstünden, hizalı bir noktadan başlatıyoruz (+8 birim yukarıdan)
            Vector3 spawnPos = targetPos + new Vector3(0, 8f, 0); 

            // Fabrikadan üret
            NodeView newBlock = blockFactory.SpawnBlock(node.Type, spawnPos, node.X, node.Y);
            _viewGrid[node.X, node.Y] = newBlock;

            // Düşme hareketini tetikle
            newBlock.MoveTo(targetPos);
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
        }
    }
}