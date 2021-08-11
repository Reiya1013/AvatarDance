using UnityEngine;
using System.Collections;

public class ProbeController : MonoBehaviour
{

    ReflectionProbe probe;
    GameObject me;

    public void SetUp(GameObject obj)
    {
        me = obj;
    }

    void Start()
    {
        this.probe = me.transform.GetComponent<ReflectionProbe>();
        Debug.Log($"ReflectionProbe {name}");
    }

    void Update()
    {

        this.probe.transform.position = new Vector3(
            Camera.main.transform.position.x,
            Camera.main.transform.position.y * -1,
            Camera.main.transform.position.z
        );
        probe.RenderProbe();
    }
}