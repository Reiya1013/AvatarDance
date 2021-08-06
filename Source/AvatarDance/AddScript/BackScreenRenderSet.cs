using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackScreenRenderSet : MonoBehaviour
{
    RenderTexture renderTexture;
    // Start is called before the first frame update
    void Start()
    {
        var g = GameObject.Find("Back Screen Camera_Sc_ForceAspectRatio");
        var cam = g.transform.GetComponent<Camera>();
        var b = GameObject.Find("Back Screen");
        var mesh = b.GetComponent<MeshRenderer>();
        renderTexture = new RenderTexture(512, 512, 24);
        cam.targetTexture = renderTexture;
        mesh.materials[0].mainTexture = renderTexture;
    }

}
