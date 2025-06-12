using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConnectedTileTerrain : MonoBehaviour
{
    public enum TerrainType { Standard, Urban }

    [Header("Grille de terrain")]
    public int gridSizeX = 10;
    public int gridSizeZ = 10;
    public float tileSize = 10f;
    public float maxHeight = 2f;

    [Header("Type de terrain")]
    public TerrainType terrainType = TerrainType.Standard;

    [Header("Paramètres urbains")]
    public int buildingCount = 10;
    public Vector2 buildingHeightRange = new Vector2(20f, 60f);
    public int buildingFootprint = 2;

    public class TileData
    {
        public Vector3 center;
        public float slope;
        public float reliability;
        public GameObject tileObject;
        public Material defaultMaterial;
    }

    public TileData[,] tileGrid;

    public GameObject GetTileGameObject(int x, int z)
    {
        if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
            return tileGrid[x, z]?.tileObject;
        return null;
    }

    public void Regenerate()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        GenerateTerrain();
    }

    void Start()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        switch (terrainType)
        {
            case TerrainType.Standard:
                GenerateStandardTerrain();
                break;
            case TerrainType.Urban:
                GenerateUrbanTerrain();
                break;
        }
    }

    void GenerateStandardTerrain()
    {
        int vertCountX = gridSizeX + 1;
        int vertCountZ = gridSizeZ + 1;
        Vector3[,] gridVertices = new Vector3[vertCountX, vertCountZ];
        tileGrid = new TileData[gridSizeX, gridSizeZ];

        for (int z = 0; z < vertCountZ; z++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                float y = Random.Range(-maxHeight, maxHeight);
                gridVertices[x, z] = new Vector3(x * tileSize, y, z * tileSize);
            }
        }

        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                Vector3 v1 = gridVertices[x, z];
                Vector3 v2 = gridVertices[x + 1, z];
                Vector3 v3 = gridVertices[x, z + 1];
                Vector3 v4 = gridVertices[x + 1, z + 1];
                CreateTile(x, z, v1, v2, v3, v4);
            }
        }
    }

    void GenerateUrbanTerrain()
    {
        tileGrid = new TileData[gridSizeX, gridSizeZ];
        Vector3[,] baseHeights = new Vector3[gridSizeX + 1, gridSizeZ + 1];

        for (int z = 0; z <= gridSizeZ; z++)
        {
            for (int x = 0; x <= gridSizeX; x++)
            {
                float y = Random.Range(-maxHeight, maxHeight); // pente naturelle du sol
                baseHeights[x, z] = new Vector3(x * tileSize, y, z * tileSize);
            }
        }

        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                Vector3 v1 = baseHeights[x, z];
                Vector3 v2 = baseHeights[x + 1, z];
                Vector3 v3 = baseHeights[x, z + 1];
                Vector3 v4 = baseHeights[x + 1, z + 1];
                CreateTile(x, z, v1, v2, v3, v4);
            }
        }

        for (int i = 0; i < buildingCount; i++)
        {
            int startX = Random.Range(0, gridSizeX - buildingFootprint);
            int startZ = Random.Range(0, gridSizeZ - buildingFootprint);
            float height = Random.Range(buildingHeightRange.x, buildingHeightRange.y);

            for (int dx = 0; dx < buildingFootprint; dx++)
            {
                for (int dz = 0; dz < buildingFootprint; dz++)
                {
                    int x = startX + dx;
                    int z = startZ + dz;

                    if (tileGrid[x, z] != null)
                        Destroy(tileGrid[x, z].tileObject);

                    Vector3 basePos = new Vector3((x + 0.5f) * tileSize, 0f, (z + 0.5f) * tileSize);
                    float groundY = baseHeights[x, z].y;
                    float buildingY = groundY + height;

                    // Crée les murs
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.position = new Vector3(basePos.x, groundY + height / 2f, basePos.z);
                    wall.transform.localScale = new Vector3(tileSize, height, tileSize);
                    wall.transform.SetParent(transform);

                    TileInfo wallInfo = wall.AddComponent<TileInfo>();
                    wallInfo.slope = 90f;
                    wallInfo.reliability = 0f;

                    tileGrid[x, z] = new TileData
                    {
                        center = wall.transform.position,
                        slope = 90f,
                        reliability = 0f,
                        tileObject = wall
                    };

                    // Crée le toit plat
                    GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    roof.transform.localScale = Vector3.one * tileSize / 10f;
                    roof.transform.position = new Vector3(basePos.x, buildingY + 0.1f, basePos.z);
                    roof.transform.SetParent(wall.transform);

                    TileInfo roofInfo = roof.AddComponent<TileInfo>();
                    roofInfo.slope = 0f;
                    roofInfo.reliability = 1f;
                }
            }
        }
    }

    void CreateTile(int x, int z, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        Vector3 center = (v1 + v2 + v3 + v4) / 4f;

        GameObject tileGO = new GameObject($"Tile_{x}_{z}");
        tileGO.transform.position = center;
        tileGO.transform.SetParent(transform);
        tileGO.layer = LayerMask.NameToLayer("Obstacle");

        MeshFilter mf = tileGO.AddComponent<MeshFilter>();
        MeshRenderer mr = tileGO.AddComponent<MeshRenderer>();
        MeshCollider mc = tileGO.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { v1 - center, v2 - center, v3 - center, v4 - center };
        mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));

        Vector3 normalA = Vector3.Cross(v2 - v1, v3 - v1);
        Vector3 normalB = Vector3.Cross(v4 - v2, v3 - v2);
        Vector3 normal = (normalA + normalB).normalized;
        if (Vector3.Dot(normal, Vector3.up) < 0) normal = -normal;

        float slope = Vector3.Angle(normal, Vector3.up);
        float reliability = slope >= 20f ? 0f : 1f - Mathf.Clamp01(slope / 20f);

        var info = tileGO.AddComponent<TileInfo>();
        info.slope = slope;
        info.reliability = reliability;

        tileGrid[x, z] = new TileData
        {
            center = center,
            slope = slope,
            reliability = reliability,
            tileObject = tileGO
        };
    }
}
