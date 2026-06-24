using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketAnimator : BoosterAnimatorBase
{
    [Header("Rocket Settings")]
    [SerializeField] private float delayBetweenBlocks = 0.04f;
    [SerializeField] private float delayAfterComplete = 1f; 
    [SerializeField] private VFXType rocketVFXType = VFXType.RocketExplosion; 
    [SerializeField] private VFXType blockDestroyVFXType = VFXType.BlockDestroy;

    public override void PlayAnimation(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        StartCoroutine(RocketSequence(sourceNode, affectedNodes, onComplete));
    }

    private IEnumerator RocketSequence(NodeView sourceNode, List<NodeView> affectedNodes, Action onComplete)
    {
        bool isPumpFinished = false;
        sourceNode.PlayPumpAnimation(() => { isPumpFinished = true; });
        yield return new WaitUntil(() => isPumpFinished);

        Vector3 centerPos = sourceNode.transform.position;
        VFXManager.Instance.PlayVFX(rocketVFXType, centerPos);
        sourceNode.gameObject.SetActive(false);

        // Merkeze olan uzaklığa göre sırala (Dalga efekti için)
        affectedNodes.Sort((nodeA, nodeB) =>
        {
            float distA = Vector3.Distance(centerPos, nodeA.transform.position);
            float distB = Vector3.Distance(centerPos, nodeB.transform.position);
            return distA.CompareTo(distB);
        });

        foreach (var node in affectedNodes)
        {
            if (node == sourceNode) continue;

            VFXManager.Instance.PlayVFX(blockDestroyVFXType, node.transform.position);
            node.gameObject.SetActive(false);

            yield return new WaitForSeconds(delayBetweenBlocks);
        }

        yield return new WaitForSeconds(delayAfterComplete);

        // Atasından gelen ortak bitiriş metodunu çağır
        FinishAnimation(affectedNodes, onComplete);
    }
}