using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    [SerializeField] private ObjectPool _pool;
    
    public NodeView SpawnBlock(Node node, Vector3 position)
    {
        NodeView nodeView = _pool.GetNode();
        nodeView.transform.position = position;
        
        // Sadece node.ColorBlock'a bakarak rengi al. 
        // Eğer bu bir Kutuysa veya Roketse, ColorBlock 'None' olacağı için beyaz dönecek.
        // Ama sorun değil, çünkü NodeView zaten Roket/Kutu modelini açtığında bu rengi kullanmayacak!
        Color blockColor = GetColorForType(node.ColorBlock);
        
        nodeView.Init(node, blockColor); 
        
        return nodeView;
    }

    private Color GetColorForType(BlockType type)
    {
        switch (type)
        {
            case BlockType.Red: return Color.red;
            case BlockType.Blue: return Color.blue;
            case BlockType.Green: return Color.green;
            case BlockType.Yellow: return Color.yellow;
            case BlockType.Purple: return new Color(0.5f, 0f, 0.5f); // Mor
            default: return Color.white; // None durumu
        }
    }



    // Balon patladığında objeyi silmeden güncellediğimiz o sihirli metod:
    public void UpdateBlockVisual(NodeView nodeView, Node node)
    {
        // Rengi doğrudan Type üzerinden al ve NodeView'a pasla
        Color blockColor = GetColorForType(node.ColorBlock);
        nodeView.UpdateVisuals(node, blockColor); 
    }
}