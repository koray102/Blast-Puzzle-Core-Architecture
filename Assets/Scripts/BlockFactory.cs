using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    [SerializeField] private ObjectPool _pool;
    
    // DEĞİŞİKLİK: Artık saf enum değil, tüm katmanları okuyabilmek için Node alıyor
    public NodeView SpawnBlock(Node node, Vector3 position)
    {
        NodeView nodeView = _pool.GetNode();
        nodeView.transform.position = position;
        
        // Hücrenin durumuna göre rengi belirle
        Color blockColor = GetColorForNode(node);
        nodeView.Init(blockColor, node.X, node.Y);
        
        return nodeView;
    }

    private Color GetColorForNode(Node node)
    {
        // ÖNCELİK SIRALAMASI: Hücrede önce ne görünmeli?
        
        // 1. Eğer hücrede sabit bir Kutu (Box) varsa kahverengi yap
        if (node.Obstacle == ObstacleType.Box)
        {
            return new Color(0.45f, 0.25f, 0.1f); // Ahşap/Kutu rengi
        }

        if (node.Obstacle == ObstacleType.Bubble) return new Color(0.7f, 0.9f, 1f);

        // 2. Eğer hücrede bir Booster varsa (Örn: Roket) camgöbeği yap
        if (node.Booster != BoosterType.None)
        {
            return Color.cyan; 
        }

        // 3. Hiçbiri yoksa normal renkli bloğu bas
        return GetColorForType(node.ColorBlock);
    }

    private Color GetColorForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.Red: return Color.red;
            case BlockType.Blue: return Color.blue;
            case BlockType.Green: return Color.green;
            case BlockType.Yellow: return Color.yellow;
            case BlockType.Purple: return new Color(0.5f, 0f, 0.5f);
            default: return Color.white;
        }
    }

    // BlockFactory.cs içine eklenecek
    public void UpdateBlockVisual(NodeView nodeView, Node node)
    {
        Color blockColor = GetColorForNode(node);
        nodeView.UpdateColor(blockColor);
    }
}