using UnityEngine;
using VRM;
using System.Collections.Generic;
using System;

public class FaceSyncController : MonoBehaviour
{
    public string[] BaseFace;
    public string[] VRMFace = Enum.GetNames(typeof(BlendShapePreset));

    public AnimationCurve weightCurve;
    Animator anim;
    public float delayWeight;
    public bool isKeepFace = false;
    float current = 0;



    //SkinnedMeshRenderer target;
    VRMBlendShapeProxy proxy;
    Dictionary<string, BlendShapeKey> clipList = new Dictionary<string, BlendShapeKey>();


   public void StartUp(GameObject VRM)
    {
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



    //アニメーションEvents側につける表情切り替え用イベントコール
    public void OnCallChangeFace(string str)
    {
        int ichecked = 0;

        for (int i = 0; i < BaseFace.Length; i++)
        {
            var animation = BaseFace[i];
            if (str == animation)
            {
                ChangeFace(i);
                break;
            }
            else if (ichecked <= BaseFace.Length)
            {
                ichecked++;
            }
            else
            {
                //str指定が間違っている時にはデフォルトで
                str = "natyural";
                ChangeFace(0);
            }
        }
    }

    string oldFace;

    void ChangeFace(int no)
    {
        if (!string.IsNullOrEmpty(oldFace))
            proxy.ImmediatelySetValue(clipList[oldFace], Mathf.Clamp01(0));
        proxy.ImmediatelySetValue(clipList[VRMFace[no].ToUpper()], Mathf.Clamp01(1));
        oldFace = VRMFace[no].ToUpper();
    }
}
