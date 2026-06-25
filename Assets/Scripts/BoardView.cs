using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    public static BoardView Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BlockFactory blockFactory;
    [SerializeField] private ObjectPool _pool; // Patlatma sonrası havuza göndermek için
    [SerializeField] private Transform BoosterAnimatorsParent;
    
    [Header("Layout Settings")]
    [SerializeField] private float spacing = 1.05f;
    
    
    [Header("Spawn Animation Settings")]
    [SerializeField] private float spawnBlockFallDelay = 0.04f;


    // General Settings
    private NodeView[,] _viewGrid;

    // Animation settings
    private HashSet<NodeView> _animatingNodes = new HashSet<NodeView>();
    private int _systemLocks = 0; // Sadece Controller'ın kullandığı genel gecikmeler için
    private Dictionary<BoosterType, BoosterAnimatorBase> _animatorDict;
    internal HashSet<NodeView> ActiveBoosterSources { get; private set; } = new HashSet<NodeView>(); // Şu anda kendi animasyonunu oynatan (şişen, dönen) booster'ların listesi (KORUMA KALKANI)


    private void Awake()
    {
        Instance = this;

        // 1. Sözlüğü başlat
        _animatorDict = new Dictionary<BoosterType, BoosterAnimatorBase>();

        // 2. OTOMATİK KEŞİF (Auto-Discovery)
        // Performans için animatörlerin BoardView ile aynı objede veya onun içinde (Child) olduğunu varsayıyoruz.
        // Eğer animatörler sahnede bambaşka yerlerde duracaksa bunu "FindObjectsOfType<BoosterAnimatorBase>()" yapabilirsin.
        BoosterAnimatorBase[] foundAnimators = BoosterAnimatorsParent.GetComponentsInChildren<BoosterAnimatorBase>();

        foreach (var animator in foundAnimators)
        {
            foreach (var type in animator.HandledTypes)
            {
                // Güvenlik Kontrolü: Eğer aynı tipi iki farklı animatör sahiplenmeye çalışıyorsa çökme, sadece uyar!
                if (!_animatorDict.ContainsKey(type))
                {
                    _animatorDict.Add(type, animator);
                    Debug.Log($"[BoardView] {type} animatörü başarıyla kaydedildi: {animator.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[BoardView] ÇAKIŞMA! {type} tipi zaten {_animatorDict[type].gameObject.name} tarafından kullanılıyor. {animator.gameObject.name} yoksayıldı!");
                }
            }
        }
    }


    public void BuildBoard(GridModel model)
    {
        _viewGrid = new NodeView[model.Width, model.Height];

        // 1. Eventlere Abone Oluyoruz
        model.OnBlocksMatched += HandleBlocksMatched;
        model.OnBlocksFell += HandleBlocksFell;
        model.OnNewBlocksSpawned += HandleNewBlocksSpawned;
        model.OnNodesUpdated += HandleNodesUpdated;
        model.OnBoosterDetonated += HandleBoosterDetonated;

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
            BoardController.Instance.Model.OnBoosterDetonated -= HandleBoosterDetonated;
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
    
    // Manuel kilitleme için yardımcı metodlar
    // --- 1. CONTROLLER İÇİN SİSTEM KİLİTLERİ ---
    public void LockState() => _systemLocks++;
    public void UnlockState() { _systemLocks--; CheckAndUnlockState(); }

    // --- 2. BLOKLAR İÇİN KİMLİKLİ KİLİTLER (Yeni eklenenler) ---
    public void LockNode(NodeView node) 
    {
        //Debug.Log($"{node} locked");
        if (node != null) _animatingNodes.Add(node);
    }

    public void UnlockNode(NodeView node) 
    {
        if (node != null && _animatingNodes.Contains(node))
        {
            //Debug.Log($"{node} released");
            _animatingNodes.Remove(node);
        }
        CheckAndUnlockState();
    }
    

    private void HandleBlocksMatched(List<Node> matchedNodes)
    {
        // 1. ADIM: GERİ TEPMELERİ UYGULA (YENİ ORTAK METOT)
        ApplyKnockback(matchedNodes);

        // 2. ADIM: PATLAYANLARI GİZLE 
        foreach (Node node in matchedNodes)
        {
            NodeView viewToHide = _viewGrid[node.X, node.Y];
            if (viewToHide != null)
            {
                _pool.ReturnNode(viewToHide); 
                _viewGrid[node.X, node.Y] = null; 
            }
        }
    }


    // Sayaç sıfırlandıysa kilidi açan merkez
    public void CheckAndUnlockState()
    {
        // HEM sistem kilidi kalmadıysa HEM DE hareket eden blok kalmadıysa kilidi aç!
        if (_systemLocks <= 0 && _animatingNodes.Count == 0)
        {
            _systemLocks = 0; // Güvenlik için eksiye düşmesini engelle
            if (BoardController.Instance != null && BoardController.Instance.State == GameState.Processing)
            {
                BoardController.Instance.SetState(GameState.WaitingForInput);
            }
        }
    }


    public void ResetAnimationCounter()
    {
        _systemLocks = 0;
        _animatingNodes.Clear();
    }


    // Controller'ın yerçekimini ne zaman başlatacağını bilmesi için durum bildirici
    public bool IsPlayingEffects()
    {
        // _systemLocks > 1 : Controller'ın en başta attığı (1) numaralı ana kilit haricinde, 
        // roket veya bomba animatörlerinin attığı ekstra kilitler varsa.
        // _animatingNodes.Count > 0 : Ekranda hala geri tepme (knockback) yapan bloklar varsa.
        return _systemLocks > 1 || _animatingNodes.Count > 0;
    }


    // MVC'ye uygun olarak, Controller sadece "Bu bloğa tıklama efekti ver" der, işi View yapar.
    public void PlayBlockClickFeedback(NodeView clickedView, System.Action onComplete)
    {
        if (clickedView != null)
        {
            clickedView.PlayPumpAnimation(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void HandleBlocksFell(List<BlockMoveData> moveDataList)
    {
        foreach (var moveData in moveDataList)
        {
            NodeView blockToMove = _viewGrid[moveData.FromX, moveData.FromY];
            if (blockToMove == null) continue;

            Vector3 targetPos = CalculateWorldPosition(moveData.ToX, moveData.ToY);

            _viewGrid[moveData.FromX, moveData.FromY] = null;
            _viewGrid[moveData.ToX, moveData.ToY] = blockToMove;
            blockToMove.UpdateCoordinates(moveData.ToX, moveData.ToY);

            LockNode(blockToMove);

            blockToMove.MoveTo(targetPos, () => 
            {
                UnlockNode(blockToMove);
            });
        }
    }


    private void HandleNewBlocksSpawned(List<Node> newNodes)
    {
        int height = BoardController.Instance.Model.Height;
        int width = BoardController.Instance.Model.Width;

        // 1. Her sütun için en alttaki hedef Y koordinatını bul
        int[] lowestYPerColumn = new int[width];
        for (int i = 0; i < width; i++)
        {
            lowestYPerColumn[i] = int.MaxValue;
        }

        foreach (var node in newNodes)
        {
            if (node.Y < lowestYPerColumn[node.X])
            {
                lowestYPerColumn[node.X] = node.Y;
            }
        }

        // 2. Blokları hizala ve şelale efektiyle düşür
        foreach (var node in newNodes)
        {
            Vector3 targetPos = CalculateWorldPosition(node.X, node.Y);
            
            int lowestY = lowestYPerColumn[node.X];
            
            // Sıralı dizilim için yükseklik farkı
            int spawnYOffset = (node.Y - lowestY) + 1;
            Vector3 spawnPos = CalculateWorldPosition(node.X, height + spawnYOffset); 

            NodeView newBlock = blockFactory.SpawnBlock(node, spawnPos);

            _viewGrid[node.X, node.Y] = newBlock;

            // ÇOK KRİTİK: Kilidi anında atıyoruz ki bekleme süresince orkestra yanlışlıkla kilidi açmasın
            LockNode(newBlock);

            // Gecikme süresi: Yukarıdaki bloklar daha geç düşmeye başlar
            float delay = (node.Y - lowestY) * spawnBlockFallDelay; 

            // Hareketi doğrudan vermek yerine, bekleme yapacak Coroutine'i başlatıyoruz
            StartCoroutine(StaggeredDropRoutine(newBlock, targetPos, delay));
        }
    }

    
    private System.Collections.IEnumerator StaggeredDropRoutine(NodeView block, Vector3 targetPos, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // Eğer bekleme süresinde obje beklenmedik bir şekilde kapanırsa hatayı önle
        if (block == null || !block.gameObject.activeInHierarchy)
        {
            UnlockNode(block);
            yield break; 
        }

        // Süre doldu, düşüşü başlat! Bittiğinde de kilidi aç.
        block.MoveTo(targetPos, () => 
        {
            UnlockNode(block);
        });
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


    private void HandleBoosterDetonated(Node sourceBooster, List<Node> affectedNodes)
    {
        NodeView sourceView = _viewGrid[sourceBooster.X, sourceBooster.Y];
        if (sourceView == null) return;
        
        // Bu kaynağı koruma altına al (Başka bir patlama bunu yanlışlıkla yok etmesin)
        ActiveBoosterSources.Add(sourceView);

        List<NodeView> affectedViews = new List<NodeView>();
        
        foreach (Node node in affectedNodes)
        {
            // Eğer objeyi başka bir patlama zaten sildiyse (null ise) pas geç
            if (_viewGrid[node.X, node.Y] == null) continue;

            NodeView view = _viewGrid[node.X, node.Y];
            affectedViews.Add(view);

            // --- İŞTE O SİHİRLİ KONTROL (RACE CONDITION ÖNLEYİCİ) ---
            // Eğer patlatacağımız bu blok DİĞER BİR BOOSTER ise, onu grid'den ŞİMDİ SİLME!
            // Bırakalım GridModel'den onun kendi patlama event'i geldiğinde kendini silsin.
            // Sadece normal blokları, kutuları ve ana kaynağın kendisini sil.
            if (node.Booster == BoosterType.None || node == sourceBooster)
            {
                _viewGrid[node.X, node.Y] = null; 
            }
        }

        // Animasyonu Oynat (Sen o önceki Invoke'u yukarı aldığın için node.Booster hala dolu geliyor!)
        if (_animatorDict.TryGetValue(sourceBooster.Booster, out BoosterAnimatorBase activeAnimator))
        {
            Debug.Log($"{activeAnimator} run lock state");
            LockState();
            activeAnimator.PlayAnimation(sourceView, affectedViews, () => 
            {
                // Animasyon bitti, kalkanı indir
                ActiveBoosterSources.Remove(sourceView);
                Debug.Log($"{activeAnimator} finish release state");
                UnlockState(); 
            });
        }
        else
        {
            ActiveBoosterSources.Remove(sourceView);
            foreach (var view in affectedViews) { _pool.ReturnNode(view); }
            if (sourceView != null) _pool.ReturnNode(sourceView);
        }
    }


    private void ApplyKnockback(List<Node> explodingNodes)
    {
        Dictionary<NodeView, Vector3> knockbackVectors = new Dictionary<NodeView, Vector3>();

        foreach (Node node in explodingNodes)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = node.X + dx[i];
                int ny = node.Y + dy[i];

                if (BoardController.Instance.Model.IsValidPosition(nx, ny))
                {
                    // Eğer komşu hücre de patlayanların içinde DEĞİLSE itilecek
                    bool isNeighborExploding = explodingNodes.Exists(n => n.X == nx && n.Y == ny);
                    if (!isNeighborExploding)
                    {
                        NodeView neighborView = _viewGrid[nx, ny];
                        
                        // Eğer hücre boş değilse ve fırıldak gibi dönen başka bir booster değilse
                        if (neighborView != null && !ActiveBoosterSources.Contains(neighborView))
                        {
                            Vector3 pushDir = new Vector3(dx[i], dy[i], 0);

                            if (!knockbackVectors.ContainsKey(neighborView))
                                knockbackVectors[neighborView] = Vector3.zero;

                            knockbackVectors[neighborView] += pushDir; 
                        }
                    }
                }
            }
        }

        // Hesaplanan itme vektörlerini GÜNCEL KİLİT MİMARİSİ ile uygula
        foreach (var kvp in knockbackVectors)
        {
            NodeView view = kvp.Key;
            Vector3 pushDirection = kvp.Value.normalized; // Çapraz tepmeler için normalize et

            // DİKKAT: Eski kör sayaç yerine, bloğun kendisini kilit listesine alıyoruz!
            LockNode(view); 

            view.PlayKnockback(pushDirection, () => 
            {
                // İşlem bitince bloğu kilit listesinden çıkarıyoruz
                UnlockNode(view); 
            });
        }
    }


    // --- YENİ: Animatörlerin kendi zamanlamasıyla çağıracağı Overload ---
    public void ApplyKnockback(List<NodeView> explodingViews)
    {
        Dictionary<NodeView, Vector3> knockbackVectors = new Dictionary<NodeView, Vector3>();

        foreach (NodeView view in explodingViews)
        {
            if (view == null) continue;

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = view.X + dx[i];
                int ny = view.Y + dy[i];

                if (BoardController.Instance.Model.IsValidPosition(nx, ny))
                {
                    bool isNeighborExploding = explodingViews.Exists(v => v != null && v.X == nx && v.Y == ny);
                    if (!isNeighborExploding)
                    {
                        NodeView neighborView = _viewGrid[nx, ny];
                        
                        // Sadece aktif olanlara ve koruma kalkanında olmayanlara şok dalgası vur
                        if (neighborView != null && neighborView.gameObject.activeInHierarchy && !ActiveBoosterSources.Contains(neighborView))
                        {
                            Vector3 pushDir = new Vector3(dx[i], dy[i], 0);

                            if (!knockbackVectors.ContainsKey(neighborView))
                                knockbackVectors[neighborView] = Vector3.zero;

                            knockbackVectors[neighborView] += pushDir; 
                        }
                    }
                }
            }
        }

        foreach (var kvp in knockbackVectors)
        {
            NodeView view = kvp.Key;
            Vector3 pushDirection = kvp.Value.normalized;

            LockNode(view); 
            view.PlayKnockback(pushDirection, () => 
            {
                UnlockNode(view); 
            });
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
            BoardController.Instance.Model.OnBoosterDetonated -= HandleBoosterDetonated;
        }
    }
}