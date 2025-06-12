using UnityEngine;

public class DroneSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject dronePrefab;
    public Transform spawnPoint;
    public ConnectedTileTerrain terrainGenerator;

    [Header("Options")]
    public bool useRandomTarget = false;

    [Header("Paramètres d'itération")]
    public int maxIterations = -1; // -1 = infini
    private int currentIteration = 0;

    private GameObject currentDrone;

    void Update()
    {
        if (currentDrone == null && (maxIterations < 0 || currentIteration < maxIterations))
        {
            GenerateTerrainAndSpawnDrone();
            currentIteration++;
        }
    }

    void GenerateTerrainAndSpawnDrone()
    {
        if (terrainGenerator != null)
        {
            terrainGenerator.Regenerate(); // 🔁 on régénère uniquement ici
        }

        if (dronePrefab != null && spawnPoint != null)
        {
            currentDrone = Instantiate(dronePrefab, spawnPoint.position, spawnPoint.rotation);

            var controller = currentDrone.GetComponent<DroneController>();
            if (controller != null)
            {
                controller.useRandomTarget = useRandomTarget;
            }
        }
    }
    
    public GameObject SpawnSingleDrone()
    {
        if (terrainGenerator != null)
            terrainGenerator.Regenerate();

        if (dronePrefab != null && spawnPoint != null)
        {
            GameObject drone = Instantiate(dronePrefab, spawnPoint.position, spawnPoint.rotation);
            var controller = drone.GetComponent<DroneController>();

            if (controller != null)
            {
                controller.useRandomTarget = useRandomTarget;
                controller.terrain = terrainGenerator;
            }

            return drone;
        }

        return null;
    }

}