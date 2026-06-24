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
    protected void FinishAnimation(List<NodeView> affectedNodes, Action onComplete)
    {
        // Blokları havuza at
        foreach (var node in affectedNodes)
        {
            if (node != null && node.gameObject.activeSelf)
            {
                _pool.ReturnNode(node); 
            }
        }

        // BoardView'a "Benim işim bitti, kilidi aç ve yerçekimini başlat" de
        onComplete?.Invoke();
    }

    // Alt sınıfların içini doldurmak zorunda olduğu ana metod
    public abstract void PlayAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete);
}