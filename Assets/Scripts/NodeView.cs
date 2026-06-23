using System.Collections;
using UnityEngine;

public class NodeView : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 10f; // Sabit düşme hızı
    

    [Header("Reference Settings")]
    [SerializeField] private MeshRenderer _renderer;

    // Draw Call optimizasyonu için PropertyBlock
    private static MaterialPropertyBlock _propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    
    public int X { get; private set; }
    public int Y { get; private set; }
    public Color CurrentColor { get; private set; }
    private Coroutine _moveRoutine;


    public void Init(Color blockColor, int x, int y)
    {
        X = x;
        Y = y;
        CurrentColor = blockColor;

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

    
    public void UpdateCoordinates(int newX, int newY)
    {
        X = newX;
        Y = newY;
    }


    public void UpdateColor(Color newColor)
    {
        CurrentColor = newColor;
        _propBlock.SetColor(ColorProperty, newColor);
        _renderer.SetPropertyBlock(_propBlock);
    }


    public void Hide()
    {
        gameObject.SetActive(false);
    }


    // Dışarıdan tetiklenecek hareket fonksiyonu
    public void MoveTo(Vector3 targetPos)
    {
        // Eğer obje zaten hareket halindeyse eski hareketi durdur (Bugları önler)
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }
        _moveRoutine = StartCoroutine(MoveRoutine(targetPos));
    }


    // Sabit hızla hedefe ilerleme mantığı
    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        // Hedef pozisyona varana kadar her frame'de çalışır
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            // MoveTowards tam olarak sabit hız (linear) sağlar
            transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
            yield return null; 
        }
        
        // Döngü bitince objeyi tam yerine oturt ve rutini temizle
        transform.position = targetPos;
        _moveRoutine = null;
    }
}