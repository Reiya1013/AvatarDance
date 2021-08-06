using UnityEngine;

class Spawner_Multi : Spawner
{
    public void StartUp(GameObject Laser,Transform[] SetPoint, GameObject parent)
    {
        this.prefabs = new GameObject[] { Laser };
        this.spawnPoints = SetPoint;
        this.parent = parent.transform;
        this.distribution = Distribution.AtPoints;
        this.spawnRateRandomness = 0.5f;
        this.spawnRate = 2.0f;


    }
}
