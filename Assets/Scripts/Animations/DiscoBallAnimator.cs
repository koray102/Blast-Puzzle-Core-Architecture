using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscoBallAnimator : BoosterAnimatorBase
{
    [Header("Disco Ball Settings")]
    [SerializeField] private float delayBetweenLines = 0.05f; // Çizgiler arası milisaniyelik bekleme
    [SerializeField] private float preExplosionDelay = 0.2f;  // Tüm çizgiler bitince patlamadan önceki "gerilim" es'i
    [SerializeField] private float delayAfterComplete = 0.25f; 
    
    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;           // Çizgi materyali (Parlak/Lazer gibi bir materyal harika durur)
    [SerializeField] private float lineWidth = 0.15f;         // Çizgi kalınlığı
    [SerializeField] private Color lineColor = Color.cyan;    // Çizgi rengi
    [SerializeField] private float lineZOffset = -1.0f;
    [SerializeField] private int lineSortingOrder = 10;

    [Header("VFX Settings")]
    [SerializeField] private VFXType discoVFXType = VFXType.DiscoBallExplosion; 
    [SerializeField] private VFXType blockDestroyVFXType = VFXType.BlockDestroy;

    public override void PlayAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        StartCoroutine(DiscoSequence(sourceNode, affectedNodes, onComplete));
    }

    private IEnumerator DiscoSequence(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        Vector3 centerPos = sourceNode.transform.position;
        
        // Çizgileri oyun bitiminde silmek için tutacağımız geçici liste
        List<GameObject> tempLines = new List<GameObject>();

        // 1. AŞAMA: LAZERLERİ ÇEK
        foreach (var node in affectedNodes)
        {
            if (node == sourceNode) continue; 

            GameObject lineObj = new GameObject($"DiscoLine_To_{node.X}_{node.Y}");
            lineObj.transform.position = centerPos;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default")); 
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.positionCount = 2; 

            // --- YENİ EKLENEN GÜVENLİK: Çizim sırasını en üste al (Sorting Order) ---
            lr.sortingOrder = lineSortingOrder;

            // --- YENİ EKLENEN KISIM: Başlangıç ve Bitiş noktalarına Z Offset uygula ---
            Vector3 startPosWithOffset = new Vector3(centerPos.x, centerPos.y, centerPos.z + lineZOffset);
            Vector3 targetPosWithOffset = new Vector3(node.transform.position.x, node.transform.position.y, node.transform.position.z + lineZOffset);

            lr.SetPosition(0, startPosWithOffset);
            lr.SetPosition(1, targetPosWithOffset);

            tempLines.Add(lineObj);

            yield return new WaitForSeconds(delayBetweenLines);
        }

        // 2. AŞAMA: GERİLİM (Tüm hedefler kilitlendi, patlamadan önce ufak bir nefes al)
        yield return new WaitForSeconds(preExplosionDelay);

        // 3. AŞAMA: ANA PATLAMA (Disko Topunun Kendisi)
        VFXManager.Instance.PlayVFX(discoVFXType, centerPos);
        sourceNode.gameObject.SetActive(false);

        // 4. AŞAMA: HEDEFLERİN PATLAMASI (Bağlanan tüm bloklar AYNI ANDA havaya uçsun)
        foreach (var node in affectedNodes)
        {
            if (node == sourceNode) continue;

            // 1. Bu blok başka bir animatörün (örneğin tetiklenen bir bombanın) ana objesiyse, ona DOKUNMA! O kendi animatörüyle patlayacak.
            if (BoardView.Instance.ActiveBoosterSources.Contains(node)) continue;

            // 2. Bu blok roketin çaprazından veya başka bir patlamadan dolayı zaten patladıysa, tekrar VFX oynatıp çorba yapma.
            if (!node.gameObject.activeInHierarchy) continue;

            VFXManager.Instance.PlayVFX(blockDestroyVFXType, node.transform.position);
            node.gameObject.SetActive(false);
        }

        // 5. TEMİZLİK AŞAMASI
        // Geçici olarak oluşturduğumuz o çizgi objelerini sahneden temizle
        foreach (var line in tempLines)
        {
            Destroy(line);
        }
        tempLines.Clear();

        yield return new WaitForSeconds(delayAfterComplete);

        // Atasından gelen ortak bitiriş metodunu çağır (Objeleri havuza at ve yerçekimini başlat)
        FinishAnimation(sourceNode, affectedNodes, onComplete);
    }
}