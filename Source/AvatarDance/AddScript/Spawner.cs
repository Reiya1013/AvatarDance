using UnityEngine;
using System.Collections;


[AddComponentMenu("Reaktion/Utility/Spawner")]
public class Spawner : MonoBehaviour
{
    // Prefabs to be spawned.
    public GameObject[] prefabs;

    // Spawn rate settings.
    public float spawnRate;
    public float spawnRateRandomness;

    // Distribution settings.
    public enum Distribution { InSphere, InBox, AtPoints }
    public Distribution distribution;

    public float sphereRadius;
    public Vector3 boxSize;
    public Transform[] spawnPoints;

    // Rotation settings.
    public bool randomRotation;

    // Parenting option.
    public Transform parent;

    // Private variables.
    float randomValue;
    float timer;
    int spawnPointIndex;

    // Spawn an instance.
    public void Spawn()
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];

        // Get an initial position and rotation.
        Vector3 position;
        Quaternion rotation;

        if (distribution == Distribution.AtPoints)
        {
            // Choose a spawn point in random order.
            spawnPointIndex += Random.Range(1, spawnPoints.Length);
            spawnPointIndex %= spawnPoints.Length;
            var pt = spawnPoints[spawnPointIndex];

            position = pt.position;
            rotation = randomRotation ? Random.rotation : prefab.transform.rotation * pt.rotation;
        }
        else
        {
            if (distribution == Distribution.InSphere)
            {
                position = transform.TransformPoint(Random.insideUnitSphere * sphereRadius);
            }
            else // Distribution.InBox
            {
                var rv = new Vector3(Random.value, Random.value, Random.value);
                position = transform.TransformPoint(Vector3.Scale(rv - Vector3.one * 0.5f, boxSize));
            }

            rotation = randomRotation ? Random.rotation : prefab.transform.rotation * transform.rotation;
        }

        // Instantiate.
        var instance = Instantiate(prefab, position, rotation) as GameObject;
        instance.transform.GetComponent<SelfDestruction>().enabled = true;

        // Parenting.
        if (parent != null) instance.transform.parent = parent;
    }

    // Make some instances.
    public void Spawn(int count)
    {
        while (count-- > 0) Spawn();
    }

    void Update()
    {
        if (spawnRate > 0.0f)
        {
            timer += Time.deltaTime;

            while (timer > (1.0f - randomValue) / spawnRate)
            {
                Spawn();
                timer -= (1.0f - randomValue) / spawnRate;
                randomValue = Random.value * spawnRateRandomness;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.5f);

        if (distribution == Distribution.AtPoints)
        {
            foreach (var pt in spawnPoints)
                Gizmos.DrawWireCube(pt.position, Vector3.one * 0.1f);
        }
        else if (distribution == Distribution.InSphere)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, sphereRadius);
        }
        else // Distribution.InBox
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
        }
    }
}

