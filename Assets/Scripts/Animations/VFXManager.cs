using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public class VFXPoolData
    {
        public VFXType type;
        public ParticleSystem prefab;
        public int initialSize = 20;
    }

    [SerializeField] private List<VFXPoolData> poolSettings;

    [Header("Global Settings")]
    [SerializeField] private float globalZOffset = -1.5f;
    
    private Dictionary<VFXType, Queue<ParticleSystem>> _vfxPools;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        _vfxPools = new Dictionary<VFXType, Queue<ParticleSystem>>();

        foreach (var setting in poolSettings)
        {
            // KORUMA 1: Eğer Unity Inspector'dan prefab atanmamışsa, bu VFX tipini atla ve hata verme.
            if (setting.prefab == null)
            {
                Debug.LogWarning($"[VFXManager] {setting.type} için Prefab atanmamış! Bu efekt oynatılmayacak.");
                continue; 
            }

            Queue<ParticleSystem> newPool = new Queue<ParticleSystem>();
            for (int i = 0; i < setting.initialSize; i++)
            {
                ParticleSystem vfx = Instantiate(setting.prefab, transform);
                vfx.gameObject.SetActive(false);
                newPool.Enqueue(vfx);
            }
            _vfxPools.Add(setting.type, newPool);
        }
    }

    public void PlayVFX(VFXType type, Vector3 position)
    {
        // KORUMA 2: Eğer o tipe ait bir havuz yoksa (prefab atanmadığı için oluşmamışsa) sessizce geri dön.
        if (!_vfxPools.ContainsKey(type)) return;

        Queue<ParticleSystem> pool = _vfxPools[type];
        
        // KORUMA 3: Havuzda eleman bittiyse ve yeni üretilecekse, prefab'ın var olduğundan emin ol.
        var setting = poolSettings.Find(s => s.type == type);
        if (pool.Count == 0 && (setting == null || setting.prefab == null)) return;

        ParticleSystem vfx = pool.Count > 0 ? pool.Dequeue() : Instantiate(setting.prefab, transform);

        // Ekstra Güvenlik
        if (vfx == null) return;

        Vector3 offsetPosition = new Vector3(position.x, position.y, position.z + globalZOffset);
        vfx.transform.position = offsetPosition;
        
        vfx.gameObject.SetActive(true);
        vfx.Play();

        StartCoroutine(ReturnVFXToPool(vfx, type, vfx.main.duration));
    }

    private IEnumerator ReturnVFXToPool(ParticleSystem vfx, VFXType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfx != null)
        {
            vfx.gameObject.SetActive(false);
            
            // Eğer havuz bir şekilde duruyorsa geri ekle
            if (_vfxPools.ContainsKey(type))
            {
                _vfxPools[type].Enqueue(vfx);
            }
        }
    }
}