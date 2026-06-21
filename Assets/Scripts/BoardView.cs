using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockFactory blockFactory;
    [SerializeField] private ObjectPool _pool;
    
    [Header("Layout Settings")]
    [SerializeField] private float spacing = 1.05f; // Küplerin arasındaki boşluk (Küp boyutu 1 birimse, 1.05 hafif boşluk bırakır)
    
    // Ekranda hangi koordinatta hangi görsel küp var takip etmek için 2D dizi
    private NodeView[,] _viewGrid;

    // Model verisi oluştuktan sonra BoardController bu fonksiyonu çağıracak
    public void BuildBoard(GridModel model)
    {
        _viewGrid = new NodeView[model.Width, model.Height];

        // Tahtayı ekranın tam ortasına hizalamak için bir offset (kaydırma) hesaplıyoruz
        Vector3 centerOffset = new Vector3(
            -(model.Width - 1) * spacing / 2f,
            -(model.Height - 1) * spacing / 2f,
            0
        );

        // Arka plandaki matrisin üzerinden geçiyoruz
        for (int x = 0; x < model.Width; x++)
        {
            for (int y = 0; y < model.Height; y++)
            {
                Node node = model.GetNode(x, y);
                
                if (node != null)
                {
                    // 1. Hücrenin dünyadaki (Unity sahnesindeki) pozisyonunu hesapla
                    Vector3 worldPosition = new Vector3(x * spacing, y * spacing, 0) + centerOffset;

                    // 2. Fabrikadan doğru renkte bir küp iste ve o pozisyona koy
                    NodeView spawnedBlock = blockFactory.SpawnBlock(node.Type, worldPosition, x, y);

                    // 3. İleride patlatırken kolayca bulabilmek için bu görseli matrise kaydet
                    // GridModel'deki tablodan ayri olarak sahnedeki objenin referansini tutar ve obje bulma hizini artirir
                    _viewGrid[x, y] = spawnedBlock;
                }
            }
        }

        model.OnBlocksMatched += HandleBlocksMatched;
    }

    private void HandleBlocksMatched(List<Node> matchedNodes)
    {
        foreach (Node node in matchedNodes)
        {
            // View matrisinden fiziksel objeyi bul
            NodeView viewToHide = _viewGrid[node.X, node.Y];
            
            if (viewToHide != null)
            {
                // Obje havuzuna geri yolla (kendi içinde SetActive(false) yapacak)
                _pool.ReturnNode(viewToHide);
                
                // Artık o hücrede görsel bir obje yok
                _viewGrid[node.X, node.Y] = null; 
            }
        }
    }

    private void OnDestroy()
    {
        // Memory leak önlemek için obje yok olduğunda aboneliği iptal et
        if (BoardController.Instance != null && BoardController.Instance.Model != null)
        {
            BoardController.Instance.Model.OnBlocksMatched -= HandleBlocksMatched;
        }
    }
}