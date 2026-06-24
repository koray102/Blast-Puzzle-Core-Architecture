using System;
using System.Collections;
using UnityEngine;

public class NodeView : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float fallSpeed = 10f; // Sabit düşme hızı
    

    [Header("3D Models (Child Objects)")]
    [SerializeField] private GameObject _colorCubeModel;
    [SerializeField] private MeshRenderer _colorCubeRenderer;
    [SerializeField] private GameObject _boxModel;
    [SerializeField] private GameObject _bubbleModel; 
    [SerializeField] private GameObject _rocketHModel;
    [SerializeField] private GameObject _rocketVModel;
    [SerializeField] private GameObject _bombModel;
    [SerializeField] private GameObject _discoModel;


    // Draw Call optimizasyonu için PropertyBlock
    private static MaterialPropertyBlock _propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    
    public int X { get; private set; }
    public int Y { get; private set; }
    public Color CurrentColor { get; private set; }
    private Coroutine _moveRoutine;
    private Action _currentCallback; // Kilitlenmeyi önleyen güvenlik ağı


    public void Init(Node node, Color blockColor)
    {
        X = node.X;
        Y = node.Y;
        CurrentColor = blockColor;
        
        transform.localScale = Vector3.one; 
        transform.localRotation = Quaternion.identity;

        // Görselleri güncelle
        UpdateVisuals(node, blockColor);
        gameObject.SetActive(true);
    }

    
    public void UpdateCoordinates(int newX, int newY)
    {
        X = newX;
        Y = newY;
    }


    // Node verisine göre hangi modelin görüneceğine karar veren ana merkez
    public void UpdateVisuals(Node node, Color blockColor)
    {
        // 1. Önce her şeyi kapatıp temiz bir sayfa açıyoruz
        _colorCubeModel.SetActive(false);
        _boxModel.SetActive(false);
        _bubbleModel.SetActive(false);
        _rocketHModel.SetActive(false);
        _rocketVModel.SetActive(false);
        _bombModel.SetActive(false);
        _discoModel.SetActive(false);

        // 2. KURAL: EĞER BALON (BUBBLE) VARSA SADECE BALONU AÇ VE ÇIK!
        // Altındaki renkli küp veya booster modeli kesinlikle kapalı kalacak.
        if (node.Obstacle == ObstacleType.Bubble)
        {
            _bubbleModel.SetActive(true);
            return; // Altındaki mekanikleri göstermemek için fonksiyonu burada bitiriyoruz
        }

        // 3. Eğer Sabit bir Kutu (Box) varsa
        if (node.Obstacle == ObstacleType.Box)
        {
            _boxModel.SetActive(true);
            return; 
        }

        // 4. Eğer Booster varsa, ilgili modeli aç
        if (node.Booster != BoosterType.None)
        {
            if (node.Booster == BoosterType.RocketHorizontal) _rocketHModel.SetActive(true);
            else if (node.Booster == BoosterType.RocketVertical) _rocketVModel.SetActive(true);
            else if (node.Booster == BoosterType.Bomb) _bombModel.SetActive(true);
            else if (node.Booster == BoosterType.DiscoBall) _discoModel.SetActive(true);
        }
        // 5. Hiçbir engel veya booster yoksa, normal renkli küpü aç
        else if (node.ColorBlock != BlockType.None)
        {
            _colorCubeModel.SetActive(true);
            
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _colorCubeRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorProperty, blockColor);
            _colorCubeRenderer.SetPropertyBlock(_propBlock);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    // Dışarıdan tetiklenecek hareket fonksiyonu
    public void MoveTo(Vector3 targetPos, Action onComplete = null)
    {
        // Eğer zaten hareket halindeyse, eski hareketi kes ama sayacın kilitlenmemesi için
        // eski callback'i çalıştırılmış say!
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            SafeExecuteCallback(); 
        }
        
        // Yeni görevi güvenli hafızaya al
        _currentCallback = onComplete;
        
        // Coroutine'i SADECE hedef pozisyon ile başlat
        _moveRoutine = StartCoroutine(MoveRoutine(targetPos));
    }

    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);
            yield return null;
        }
        
        transform.position = targetPos; // Tam hedefe oturt
        
        // Animasyon bitti! Hafızadaki görev neyse onu güvenlice çalıştır ve temizle
        SafeExecuteCallback();
    }

    public void PlayPumpAnimation(Action onComplete)
    {
        _currentCallback = onComplete;
        StartCoroutine(PumpRoutine());
    }

    private IEnumerator PumpRoutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.25f; // %25 Büyü

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f); // Şişik halde çok kısa bekleme
        
        transform.localScale = originalScale; // Havuza gitmeden önce boyutu eski haline getir
        SafeExecuteCallback();
    }

    // --- GERİ TEPME (KNOCKBACK) ANİMASYONU ---
    public void PlayKnockback(Vector3 direction, Action onComplete)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        SafeExecuteCallback(); // Varsa eski hareketi bitmiş say

        _currentCallback = onComplete;
        _moveRoutine = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        Vector3 originalPos = transform.position;
        Vector3 targetPos = originalPos + (direction * 0.25f); // Yön vektörüne doğru 0.25 birim itil

        // Hızlıca geriye sek
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            transform.position = Vector3.Lerp(originalPos, targetPos, t);
            yield return null;
        }

        // Yumuşakça geri dön
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            transform.position = Vector3.Lerp(targetPos, originalPos, t);
            yield return null;
        }

        transform.position = originalPos;
        SafeExecuteCallback();
    }
    
    private void SafeExecuteCallback()
    {
        if (_currentCallback != null)
        {
            var temp = _currentCallback;
            _currentCallback = null;
            temp.Invoke();
        }
    }
    

    private void OnDisable()
    {
        // EĞER obje bir şekilde havuza dönerse ve üzerinde yarım kalan bir callback varsa, 
        // sistemi kilitli bırakmamak için callback'i zorla tetikle (Güvenlik ağı)
        SafeExecuteCallback();
    }
}