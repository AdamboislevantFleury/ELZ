using UnityEngine;

public class DroneSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject dronePrefab;
    public Transform spawnPoint;
    public ConnectedTileTerrain terrainGenerator;

    [Header("Options")]
    public bool useRandomTarget = false;

    private GameObject currentDrone;

    void Start()
    {
        // On n'appelle pas la génération ici, car elle est déjà faite.
    }

    void Update()
    {
        if (currentDrone == null)
        {
            GenerateTerrainAndSpawnDrone();
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
}
