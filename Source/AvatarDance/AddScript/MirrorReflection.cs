using UnityEngine;
using System.Collections;
using UnityEngine.XR;
//using XRSettings = UnityEngine.VR.VRSettings;
//using XRDevice = UnityEngine.VR.VRDevice;

[ExecuteInEditMode]
public class MirrorReflection : MonoBehaviour
{
    public Material m_matCopyDepth;
    public bool m_DisablePixelLights = true;
    public int m_TextureSize = 256;
    public float m_ClipPlaneOffset = 0.07f;

    public LayerMask m_ReflectLayers = -1;

    private Hashtable m_ReflectionCamerasLeft = new Hashtable(); // Camera -> Camera table
    private Hashtable m_ReflectionCamerasRight = new Hashtable(); // Camera -> Camera table

    public RenderTexture m_ReflectionTextureLeft = null;
    public RenderTexture m_ReflectionTextureRight = null;
    public RenderTexture m_ReflectionDepthTexture = null;
    private int m_OldReflectionTextureSize = 0;

    private static bool s_InsideRendering = false;


    public void OnWillRenderObject()
    {
        if (!enabled || !GetComponent<Renderer>() || !GetComponent<Renderer>().sharedMaterial || !GetComponent<Renderer>().enabled)
            return;

        Camera cam = Camera.current;
        if (!cam)
            return;




        // Safeguard from recursive reflections.
        if (s_InsideRendering)
            return;
        s_InsideRendering = true;

        Camera reflectionCameraLeft;
        Camera reflectionCameraRight;
        CreateMirrorObjects(cam, out reflectionCameraLeft, out reflectionCameraRight);

        // find out the reflection plane: position and normal in world space
        Vector3 pos = transform.position;
        Vector3 normal = transform.up;

        // Optionally disable pixel lights for reflection
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (m_DisablePixelLights)
            QualitySettings.pixelLightCount = 0;

        if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
        {
            UpdateCameraModes(cam, reflectionCameraLeft);
            RenderReflection(pos, normal, cam, reflectionCameraLeft, ref m_ReflectionTextureLeft);

        }
        if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
        {
            UpdateCameraModes(cam, reflectionCameraRight);
            RenderReflection(pos, normal, cam, reflectionCameraRight, ref m_ReflectionTextureRight);

        }




        //GL.SetRevertBackfacing(false);
        GL.invertCulling = false;
        Material[] materials = GetComponent<Renderer>().sharedMaterials;
        foreach (Material mat in materials)
        {
            if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Left)
                mat.SetTexture("_ReflectionTex", m_ReflectionTextureLeft);
            if (cam.stereoTargetEye == StereoTargetEyeMask.Both || cam.stereoTargetEye == StereoTargetEyeMask.Right)
                mat.SetTexture("_ReflectionTex", m_ReflectionTextureRight);

            mat.SetTexture("_ReflectionDepthTex", m_ReflectionDepthTexture);
        }

        // // Set matrix on the shader that transforms UVs from object space into screen
        // // space. We want to just project reflection texture on screen.
        // Matrix4x4 scaleOffset = Matrix4x4.TRS(
        // 	new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        // Vector3 scale = transform.lossyScale;
        // Matrix4x4 mtx = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z));
        // mtx = scaleOffset * cam.projectionMatrix * cam.worldToCameraMatrix * mtx;
        // foreach (Material mat in materials)
        // {
        // 	mat.SetMatrix("_ProjMatrix", mtx);
        // }

        // Restore pixel light count
        if (m_DisablePixelLights)
            QualitySettings.pixelLightCount = oldPixelLightCount;

        s_InsideRendering = false;
    }

    void RenderReflection(Vector3 pos, Vector3 normal, Camera cam, Camera reflectionCamera, ref RenderTexture renderTexture)
    {

        // Render reflection
        // Reflect camera around reflection plane
        float d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflection, reflectionPlane);
        Vector3 oldpos = cam.transform.position;
        Vector3 newpos = reflection.MultiplyPoint(oldpos);
        reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
        Matrix4x4 projection = cam.projectionMatrix;
        CalculateObliqueMatrix(ref projection, clipPlane);
        reflectionCamera.projectionMatrix = projection;

        reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value; // never render water layer
        reflectionCamera.targetTexture = renderTexture;
        //GL.SetRevertBackfacing(true);
        GL.invertCulling = true;
        reflectionCamera.transform.position = newpos;
        Vector3 euler = cam.transform.eulerAngles;
        reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
        reflectionCamera.depthTextureMode = DepthTextureMode.Depth;
        reflectionCamera.Render();

        // copy depth
        Graphics.SetRenderTarget(m_ReflectionDepthTexture);
        m_matCopyDepth.SetPass(0);
        DrawFullscreenQuad();
        Graphics.SetRenderTarget(null);
        // Graphics.Blit(m_ReflectionTexture, m_ReflectionDepthTexture, m_matCopyDepth);


        reflectionCamera.transform.position = oldpos;
    }


    // Cleanup all the objects we possibly have created
    void OnDisable()
    {
        if (m_ReflectionTextureLeft)
        {
            DestroyImmediate(m_ReflectionTextureLeft);
            m_ReflectionTextureLeft = null;
        }
        if (m_ReflectionTextureRight)
        {
            DestroyImmediate(m_ReflectionTextureRight);
            m_ReflectionTextureRight = null;
        }
        if (m_ReflectionDepthTexture)
        {
            DestroyImmediate(m_ReflectionDepthTexture);
            m_ReflectionDepthTexture = null;
        }
        foreach (DictionaryEntry kvp in m_ReflectionCamerasLeft)
            DestroyImmediate(((Camera)kvp.Value).gameObject);
        m_ReflectionCamerasLeft.Clear();

        foreach (DictionaryEntry kvp in m_ReflectionCamerasRight)
            DestroyImmediate(((Camera)kvp.Value).gameObject);
        m_ReflectionCamerasRight.Clear();
    }


    private void UpdateCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
            return;
        // set camera to clear the same way as current camera
        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }

    // On-demand create any objects we need
    private void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCameraLeft, out Camera reflectionCameraRight)
    {
        reflectionCameraLeft = null;
        reflectionCameraRight = null;

        // Reflection render texture
        if (!m_ReflectionTextureLeft || !m_ReflectionTextureRight || m_OldReflectionTextureSize != m_TextureSize)
        {
            if (m_ReflectionTextureLeft)
                DestroyImmediate(m_ReflectionTextureLeft);
            m_ReflectionTextureLeft = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            m_ReflectionTextureLeft.name = "__MirrorReflectionLeft" + GetInstanceID();
            m_ReflectionTextureLeft.isPowerOfTwo = true;
            m_ReflectionTextureLeft.hideFlags = HideFlags.DontSave;
            m_ReflectionTextureLeft.filterMode = FilterMode.Bilinear;

            if (m_ReflectionTextureRight)
                DestroyImmediate(m_ReflectionTextureRight);
            m_ReflectionTextureRight = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            m_ReflectionTextureRight.name = "__MirrorReflectionRight" + GetInstanceID();
            m_ReflectionTextureRight.isPowerOfTwo = true;
            m_ReflectionTextureRight.hideFlags = HideFlags.DontSave;
            m_ReflectionTextureRight.filterMode = FilterMode.Bilinear;

            if (m_ReflectionDepthTexture)
                DestroyImmediate(m_ReflectionDepthTexture);
            m_ReflectionDepthTexture = new RenderTexture(m_TextureSize, m_TextureSize, 0, RenderTextureFormat.RHalf);
            // m_ReflectionDepthTexture = new RenderTexture(m_TextureSize, m_TextureSize, 0, RenderTextureFormat.R8);
            m_ReflectionDepthTexture.name = "__MirrorReflectionDepth" + GetInstanceID();
            m_ReflectionDepthTexture.isPowerOfTwo = true;
            m_ReflectionDepthTexture.hideFlags = HideFlags.DontSave;
            m_ReflectionDepthTexture.filterMode = FilterMode.Bilinear;

            m_OldReflectionTextureSize = m_TextureSize;
        }

        // Camera for reflection
        reflectionCameraLeft = m_ReflectionCamerasLeft[currentCamera] as Camera;
        if (!reflectionCameraLeft) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject("Mirror Refl Camera Left id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
            reflectionCameraLeft = go.GetComponent<Camera>();
            reflectionCameraLeft.enabled = false;
            reflectionCameraLeft.transform.position = transform.position + CamOffsetPosition(currentCamera, Camera.StereoscopicEye.Left);
            reflectionCameraLeft.transform.rotation = transform.rotation * CamOffsetRotation(currentCamera, Camera.StereoscopicEye.Left);
            reflectionCameraLeft.gameObject.AddComponent<FlareLayer>();
            go.hideFlags = HideFlags.HideAndDontSave;
            m_ReflectionCamerasLeft[currentCamera] = reflectionCameraLeft;
        }

        reflectionCameraRight = m_ReflectionCamerasRight[currentCamera] as Camera;
        if (!reflectionCameraRight) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject("Mirror Refl Camera Left id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
            reflectionCameraRight = go.GetComponent<Camera>();
            reflectionCameraRight.enabled = false;
            reflectionCameraRight.transform.position = transform.position + CamOffsetPosition(currentCamera, Camera.StereoscopicEye.Right);
            reflectionCameraRight.transform.rotation = transform.rotation * CamOffsetRotation(currentCamera, Camera.StereoscopicEye.Right);
            reflectionCameraRight.gameObject.AddComponent<FlareLayer>();
            go.hideFlags = HideFlags.HideAndDontSave;
            m_ReflectionCamerasRight[currentCamera] = reflectionCameraRight;
        }

    }

    private Vector3 CamOffsetPosition(Camera currentCamera, Camera.StereoscopicEye eye)
    {

        Vector3 eyeOffset;
        if (currentCamera.stereoEnabled)
        {
            if (eye == Camera.StereoscopicEye.Left)
                eyeOffset = InputTracking.GetLocalPosition(XRNode.LeftEye);
            //var t = new SteamVR_Utils.RigidTransform(hmd.GetEyeToHeadTransform(EVREye.Eye_Left));
            else
                eyeOffset = InputTracking.GetLocalPosition(XRNode.RightEye);
            eyeOffset.z = 0.0f;
            return eyeOffset;
        }
        else
        {
            return Vector3.zero;
        }

    }
    private Quaternion CamOffsetRotation(Camera currentCamera, Camera.StereoscopicEye eye)
    {

        Quaternion eyeOffset;
        if (currentCamera.stereoEnabled)
        {
            if (eye == Camera.StereoscopicEye.Left)
                eyeOffset = InputTracking.GetLocalRotation(XRNode.LeftEye);
            else
                eyeOffset = UnityEngine.XR.InputTracking.GetLocalRotation(XRNode.RightEye);
            eyeOffset.z = 0.0f;
            return eyeOffset;
        }
        else
        {
            return Quaternion.identity;
        }

    }
    // Extended sign: returns -1, 0 or 1 based on sign of a
    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }

    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    // Adjusts the given projection matrix so that near plane is the given clipPlane
    // clipPlane is given in camera space. See article in Game Programming Gems 5 and
    // http://aras-p.info/texts/obliqueortho.html
    private static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(
            sgn(clipPlane.x),
            sgn(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        // third row = clip plane - fourth row
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    // Calculates reflection matrix around the given plane
    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }

    static public void DrawFullscreenQuad(float z = 1.0f)
    {
        GL.Begin(GL.QUADS);
        GL.Vertex3(-1.0f, -1.0f, z);
        GL.Vertex3(1.0f, -1.0f, z);
        GL.Vertex3(1.0f, 1.0f, z);
        GL.Vertex3(-1.0f, 1.0f, z);

        GL.Vertex3(-1.0f, 1.0f, z);
        GL.Vertex3(1.0f, 1.0f, z);
        GL.Vertex3(1.0f, -1.0f, z);
        GL.Vertex3(-1.0f, -1.0f, z);
        GL.End();
    }
}
