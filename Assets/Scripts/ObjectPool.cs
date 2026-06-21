using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private NodeView blockPrefab;
    [SerializeField] private int initialSize = 100; // Tahta 8x10 ise 80 yeterli ama pay bırakıyoruz
    
    private Queue<NodeView> _pool = new Queue<NodeView>();

    private void Awake()
    {
        // Oyun başlarken havuzu dolduruyoruz
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewNode();
        }
    }

    private void CreateNewNode()
    {
        NodeView node = Instantiate(blockPrefab, transform);
        node.Hide(); // Görünmez yap
        _pool.Enqueue(node); // Havuza at
    }

    // İhtiyacımız olduğunda havuzdan obje çekmek için
    public NodeView GetNode()
    {
        // Havuz boşaldıysa acil durum olarak yeni üret
        if (_pool.Count == 0) 
        {
            CreateNewNode();
        }
        
        return _pool.Dequeue();
    }

    // Patlayan/yok olan objeyi havuza geri göndermek için
    public void ReturnNode(NodeView node)
    {
        node.Hide();
        _pool.Enqueue(node);
    }
}