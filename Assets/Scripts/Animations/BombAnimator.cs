using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombAnimator : BoosterAnimatorBase
{
    [Header("Bomb Settings")]
    [SerializeField] private float delayAfterComplete = 0.25f; 
    [SerializeField] private float spinDuration = 0.6f; // Patlamadan önceki dönme/şişme süresi
    [SerializeField] private float spinSpeed = 1500f;   // Dönüş hızı (Fırıldak gibi dönmesi için yüksek tuttum)
    [SerializeField] private VFXType bombVFXType = VFXType.BombExplosion; 
    [SerializeField] private VFXType blockDestroyVFXType = VFXType.BlockDestroy;

    public override void PlayAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        StartCoroutine(BombSequence(sourceNode, affectedNodes, onComplete));
    }

    private IEnumerator BombSequence(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        // 1. HAZIRLIK AŞAMASI: Bomba patlamadan önce kendi etrafında hızla dönsün ve biraz şişsin
        float t = 0;
        Vector3 originalScale = sourceNode.transform.localScale;
        Vector3 targetScale = originalScale * 1.3f; // %30 oranında şişecek

        while (t < spinDuration)
        {
            t += Time.deltaTime;
            
            // Kendi etrafında döndür 
            // (Eğer oyunun 3D perspektifliyse Vector3.up (Y ekseni), 2D/Ortografik bakışsa Vector3.forward (Z ekseni) kullanabilirsin)
            sourceNode.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime); 
            
            // Aynı anda yumuşakça şişir
            sourceNode.transform.localScale = Vector3.Lerp(originalScale, targetScale, t / spinDuration);
            
            yield return null;
        }

        // 2. ANA PATLAMA: Bombanın kendi merkezindeki devasa partikülü oynat
        Vector3 centerPos = sourceNode.transform.position;
        VFXManager.Instance.PlayVFX(bombVFXType, centerPos);
        
        // Bombayı sahnede anında gizle
        sourceNode.gameObject.SetActive(false);

        // 3. TOPLU YIKIM: Etkilenen tüm blokları AYNI ANDA, bekleme (delay) olmadan patlat
        foreach (var node in affectedNodes)
        {
            if (node == sourceNode) continue; // Kendini tekrar patlatma

            // 1. Bu blok başka bir animatörün (örneğin tetiklenen bir bombanın) ana objesiyse, ona DOKUNMA! O kendi animatörüyle patlayacak.
            if (BoardView.Instance.ActiveBoosterSources.Contains(node)) continue;

            // 2. Bu blok roketin çaprazından veya başka bir patlamadan dolayı zaten patladıysa, tekrar VFX oynatıp çorba yapma.
            if (!node.gameObject.activeInHierarchy) continue;

            // Kutunun/Bloğun olduğu yerde parçalanma efektini oynat
            VFXManager.Instance.PlayVFX(blockDestroyVFXType, node.transform.position);

            // Bloğu anında gizle
            node.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(delayAfterComplete);

        // 4. TEMİZLİK: Atasından gelen ortak bitiriş metodunu çağır (Objeleri havuza at ve yerçekimini başlat)
        FinishAnimation(sourceNode, affectedNodes, onComplete);
    }
}