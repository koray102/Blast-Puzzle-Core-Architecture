using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoosterAnimatorBase : MonoBehaviour
{
    [SerializeField] private ObjectPool _pool;

    [Header("Registration Settings")]
    [Tooltip("Bu animatörün hangi patlayıcı tiplerinde çalışacağını seçin.")]
    [SerializeField] private List<BoosterType> handledTypes = new List<BoosterType>();
    public List<BoosterType> HandledTypes => handledTypes;

    // Bütün booster'ların animasyon bitince yapacağı ortak temizlik işlemi
    // Parametreye NodeView sourceNode EKLENDİ!
    // Parametreye NodeView sourceNode EKLENDİ!
    protected void FinishAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        foreach (var node in affectedNodes)
        {
            // Eğer bu node, ŞU ANDA animasyonu oynayan DİĞER bir booster ise onu havuza ATMA!
            if (node != null && node != sourceNode && BoardView.Instance.ActiveBoosterSources.Contains(node))
            {
                continue;
            }

            // Çifte havuzlamayı önlemek için aktiflik kontrolü
            if (node != null && node.gameObject.activeInHierarchy)
            {
                _pool.ReturnNode(node); 
            }
        }

        // Kendi objemizi de işimiz bittiği için havuza güvenle atabiliriz
        if (sourceNode != null && sourceNode.gameObject.activeInHierarchy)
        {
            _pool.ReturnNode(sourceNode);
        }

        onComplete?.Invoke();
    }

    // Alt sınıfların içini doldurmak zorunda olduğu ana metod
    public abstract void PlayAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete);
}