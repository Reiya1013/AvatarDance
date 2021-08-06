using UnityEngine;
using System.Collections;
using VRM;

public class LipSyncController : MonoBehaviour
{
    public string targetName;

    public Transform nodeA;
    public Transform nodeE;
    public Transform nodeI;
    public Transform nodeO;
    public Transform nodeU;

    public BlendShapePreset BlendShapeA;
    public BlendShapePreset BlendShapeE;
    public BlendShapePreset BlendShapeI;
    public BlendShapePreset BlendShapeO;
    public BlendShapePreset BlendShapeU;

    public AnimationCurve weightCurve;



    //SkinnedMeshRenderer target;
    VRMBlendShapeProxy proxy;

    public void StartUp(GameObject VRM)
    {
        //var tagetGameObject = GameObject.Find(targetName);
        Debug.LogWarning($"LipSyncController tagetGameObject {VRM is null} {targetName} ");
        //target = tagetGameObject.GetComponent<SkinnedMeshRenderer>();
        proxy = VRM.GetComponent<VRMBlendShapeProxy>();
        Debug.LogWarning($"LipSyncController tagetGameObject {proxy is null}");
    }

    float GetWeight(Transform tr)
    {
        return weightCurve.Evaluate(tr.localPosition.z);
    }

     void LateUpdate()
    {
        if (!proxy) return;
        var total = 1.0f;//100.0f;

        var w = total * GetWeight(nodeA);
        //target.SetBlendShapeWeight(1, Mathf.Clamp01(w));
        proxy.SetValue(BlendShapeA, Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeI);
        //target.SetBlendShapeWeight(2, Mathf.Clamp01(w));
        proxy.SetValue(BlendShapeI, Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeU);
        //target/*.Set*/BlendShapeWeight(3, Mathf.Clamp01(w));
        proxy.SetValue(BlendShapeU, Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeE);
        //target.SetBlendShapeWeight(4, Mathf.Clamp01(w));
        proxy.SetValue(BlendShapeE, Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeO);
        //target.SetBlendShapeWeight(5, Mathf.Clamp01(w));
        proxy.SetValue(BlendShapeO, Mathf.Clamp01(w));
    }
}
