using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    [SerializeField] private ObjectPool _pool;
    
    // Modelden gelen BlockType'ı alıp ekranda fiziksel bir küp oluşturur
    public NodeView SpawnBlock(BlockType type, Vector3 position, int x, int y)
    {
        // 1. Havuzdan bir küp çek
        NodeView node = _pool.GetNode();
        
        // 2. Pozisyonunu ayarla
        node.transform.position = position;
        
        // 3. Enum'a göre rengini bul ve initialize et
        Color blockColor = GetColorForType(type);
        node.Init(blockColor, x, y);
        
        return node;
    }

    // Enum değerlerini Unity Color değerlerine eşlediğimiz yer
    private Color GetColorForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.Red: return Color.red;
            case BlockType.Blue: return Color.blue;
            case BlockType.Green: return Color.green;
            case BlockType.Yellow: return Color.yellow;
            case BlockType.Purple: return new Color(0.5f, 0f, 0.5f); // Mor
            default: return Color.white;
        }
    }
}