using UnityEngine;

public class NodeView : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    public int X { get; private set; }
    public int Y { get; private set; }
    
    // Draw Call optimizasyonu için PropertyBlock
    private static MaterialPropertyBlock _propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor"); 

    public void Init(Color blockColor, int x, int y)
    {
        X = x;
        Y = y;

        // Property block sadece bir kere yaratılır, tüm objeler bunu paylaşır
        if (_propBlock == null)
        {
            _propBlock = new MaterialPropertyBlock();
        }
        
        // Rengi ayarla ve materyalin kopyasını oluşturmadan renderera bas
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(ColorProperty, blockColor);
        _renderer.SetPropertyBlock(_propBlock);
        
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}