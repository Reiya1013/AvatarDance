using UnityEngine;
using VRM;
using System.Collections.Generic;
using System;

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
    Dictionary<string, BlendShapeKey> clipList = new Dictionary<string, BlendShapeKey>();

    public void StartUp(GameObject VRM)
    {
        //var tagetGameObject = GameObject.Find(targetName);
        //target = tagetGameObject.GetComponent<SkinnedMeshRenderer>();
        proxy = VRM.GetComponent<VRMBlendShapeProxy>();

        foreach (var clip in proxy.BlendShapeAvatar.Clips)
        {
            clipList[clip.Key.Name] = clip.Key;

            // UniVRM 0.53以前は大文字に変換される
            clipList[clip.Key.Name.ToUpper()] = clip.Key;
        }
    }

    float GetWeight(Transform tr)
    {
        return weightCurve.Evaluate(tr.localPosition.z);
    }

     void LateUpdate()
    {
        if (!proxy) return;
        var total = 1.0f;

        var w = total * GetWeight(nodeA);
        //target.SetBlendShapeWeight(1, Mathf.Clamp01(w));
        //proxy.AccumulateValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeA).ToUpper()], (w));
        //proxy.SetValue(BlendShapeA, Mathf.Clamp01(w));
        proxy.ImmediatelySetValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeA).ToUpper()], Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeI);
        //target.SetBlendShapeWeight(2, Mathf.Clamp01(w));
        //proxy.AccumulateValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeI).ToUpper()], (w));
        //proxy.SetValue(BlendShapeI, Mathf.Clamp01(w));
        proxy.ImmediatelySetValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeI).ToUpper()], Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeU);
        //target/*.Set*/BlendShapeWeight(3, Mathf.Clamp01(w));
        //proxy.AccumulateValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeU).ToUpper()], (w));
        //proxy.SetValue(BlendShapeU, Mathf.Clamp01(w));
        proxy.ImmediatelySetValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeU).ToUpper()], Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeE);
        //target.SetBlendShapeWeight(4, Mathf.Clamp01(w));
        //proxy.AccumulateValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeE).ToUpper()], (w));
        //proxy.SetValue(BlendShapeE, Mathf.Clamp01(w));
        proxy.ImmediatelySetValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeE).ToUpper()], Mathf.Clamp01(w));
        total -= w;

        w = total * GetWeight(nodeO);
        //target.SetBlendShapeWeight(5, Mathf.Clamp01(w));
        //proxy.AccumulateValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeO).ToUpper()], (w));
        //proxy.SetValue(BlendShapeO, Mathf.Clamp01(w));
        proxy.ImmediatelySetValue(clipList[Enum.GetName(typeof(BlendShapePreset), BlendShapeO).ToUpper()], Mathf.Clamp01(w));
    }
}
