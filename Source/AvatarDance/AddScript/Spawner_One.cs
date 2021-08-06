using UnityEngine;

class Spawner_One : Spawner
{
    public void StartUp(GameObject Laser)
    {
        this.prefabs = new GameObject[]{ Laser };
        this.spawnPoints = new Transform[] { this.gameObject.transform };
        this.parent = this.gameObject.transform;
        this.distribution = Distribution.AtPoints;
        this.spawnRateRandomness = 0.2f;
        this.spawnRate = 0.5f;


    }
}
