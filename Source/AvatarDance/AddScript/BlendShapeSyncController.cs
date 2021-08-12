using UnityEngine;
using VRM;
using System.Collections.Generic;
using System;
using UnityEditor;

public class BlendShapeSyncController : MonoBehaviour
{
    public string targetName;

    public string[] BaseFace;
    public string[] VRMFace = Enum.GetNames(typeof(BlendShapePreset));

    public AnimationCurve weightCurve;
    public float delayWeight;
    public bool isKeepFace = false;


    //ベースAvatar側
    SkinnedMeshRenderer face;
    Dictionary<string, Int32> faceBlendIndex = new Dictionary<string, Int32>();
    Dictionary<string, float> oldWeight = new Dictionary<string, float>();
    //VRM側
    VRMBlendShapeProxy proxy;
    Dictionary<string, BlendShapeKey> clipList = new Dictionary<string, BlendShapeKey>();

    public void StartUp(GameObject VRM)
    {
        //VRM関係
        //target = tagetGameObject.GetComponent<SkinnedMeshRenderer>();
        proxy = VRM.GetComponent<VRMBlendShapeProxy>();

        foreach (var clip in proxy.BlendShapeAvatar.Clips)
        {
            clipList[clip.Key.Name] = clip.Key;

            // UniVRM 0.53以前は大文字に変換される
            clipList[clip.Key.Name.ToUpper()] = clip.Key;
        }

        //ベース側用
        face = transform.GetComponent<SkinnedMeshRenderer>();
        foreach (var name in BaseFace)
        {
            faceBlendIndex[name] = face.sharedMesh.GetBlendShapeIndex(name);
            oldWeight[name] = 0f;
        }

    }

    void Start()
    {
        //var tagetGameObject = GameObject.Find(targetName);
        ////target = tagetGameObject.GetComponent<SkinnedMeshRenderer>();
        //proxy = tagetGameObject.GetComponent<VRMBlendShapeProxy>();

        ////VRMBlensShapeを先に一覧とっておく
        //foreach (var clip in proxy.BlendShapeAvatar.Clips)
        //{
        //    clipList[clip.Key.Name] = clip.Key;

        //    // UniVRM 0.53以前は大文字に変換される
        //    clipList[clip.Key.Name.ToUpper()] = clip.Key;
        //}

        ////ベースとなるAvatarのfaceのSkinmeshRendereを取得する
        //face = transform.GetComponent<SkinnedMeshRenderer>();
        //foreach (var name in animations)
        //{
        //    faceBlendIndex[name] = face.sharedMesh.GetBlendShapeIndex(name);
        //    oldWeight[name] = 0f;
        //}
    }




    //アニメーションEvents側につける表情切り替え用イベントコール
    public void OnCallChangeFace(string str, float weight)
    {
        int ichecked = 0;

        for (int i = 0; i < BaseFace.Length; i++)
        {
            var animation = BaseFace[i];
            if (animation.Contains(str))
            {
                ChangeFace(i, weight);
                break;
            }
            else if (ichecked <= BaseFace.Length)
            {
                ichecked++;
            }
            else
            {
                //str指定が間違っている時にはデフォルトで
                str = "Neutral";
                ChangeFace(VRMFace.Length - 1, 1f);
            }
        }
    }

    string oldFace;

    void ChangeFace(int no, float weight)
    {
        //if (!string.IsNullOrEmpty(oldFace))
        //    proxy.ImmediatelySetValue(clipList[oldFace], Mathf.Clamp01(0));
        proxy.ImmediatelySetValue(clipList[VRMFace[no].ToUpper()], Mathf.Clamp01(weight));
        oldFace = VRMFace[no].ToUpper();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var name in BaseFace)
        {
            var Weight = face.GetBlendShapeWeight(faceBlendIndex[name]);
            if (oldWeight[name] != Weight)
                OnCallChangeFace(name, Weight / 100);
            oldWeight[name] = Weight;
        }
    }
}
