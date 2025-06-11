using UnityEngine;

public class DroneController : MonoBehaviour
{
    public bool useRandomTarget = false;
    private ConnectedTileTerrain terrain;
    private ConnectedTileTerrain.TileData targetTile;

    public float moveSpeed = 10f;
    public float arrivalThreshold = 0.5f;

    public static int successfulLandingsMostReliable = 0;
    public static int successfulLandingsRandom = 0;

    void Start()
    {
        terrain = FindObjectOfType<ConnectedTileTerrain>();

        if (terrain == null)
        {
            Debug.LogError("ConnectedTileTerrain not found in the scene.");
            return;
        }

        // Choisir la méthode
        if (useRandomTarget)
        {
            SetRandomTargetTile();
        }
        else
        {
            SetMostReliableTargetTile();
        }
    }

    void Update()
    {
        if (targetTile == null) return;

        Vector3 target = targetTile.center;
        Vector3 move = (target - transform.position).normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Atterrissage
        if (Vector3.Distance(transform.position, target) < arrivalThreshold)
        {
            if (useRandomTarget)
                successfulLandingsRandom++;
            else
                successfulLandingsMostReliable++;

            Destroy(gameObject);
        }
    }

    public void SetMostReliableTargetTile()
    {
        if (terrain == null || terrain.tileGrid == null) return;

        float maxReliability = -1f;

        for (int x = 0; x < terrain.gridSizeX; x++)
        {
            for (int z = 0; z < terrain.gridSizeZ; z++)
            {
                var tile = terrain.tileGrid[x, z];
                if (tile != null && tile.reliability > maxReliability)
                {
                    maxReliability = tile.reliability;
                    targetTile = tile;
                }
            }
        }
    }

    public void SetRandomTargetTile()
    {
        if (terrain == null || terrain.tileGrid == null) return;

        int maxX = terrain.gridSizeX;
        int maxZ = terrain.gridSizeZ;

        for (int i = 0; i < 50; i++)
        {
            int x = Random.Range(0, maxX);
            int z = Random.Range(0, maxZ);
            var tile = terrain.tileGrid[x, z];

            if (tile != null && tile.reliability > 0.01f)
            {
                targetTile = tile;
                return;
            }
        }

        // Fallback
        SetMostReliableTargetTile();
    }
}
