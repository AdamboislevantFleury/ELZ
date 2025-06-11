using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConnectedTileTerrain : MonoBehaviour
{
    [Header("Grille de terrain")]
    public int gridSizeX = 10;
    public int gridSizeZ = 10;
    public float tileSize = 10f;
    public float maxHeight = 2f;

    public class TileData
    {
        public int topLeftIndex;
        public int topRightIndex;
        public int bottomLeftIndex;
        public int bottomRightIndex;
        public Vector3 center;
        public float slope;         // pente moyenne
        public float reliability;   // fiabilité d'atterrissage (0-1)
        public GameObject tileObject; // GameObject associé à cette tuile
        public Material defaultMaterial;
    }

    public TileData[,] tileGrid; // Accessible depuis d'autres scripts

    public GameObject GetTileGameObject(int x, int z)
    {
        if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
            return tileGrid[x, z]?.tileObject;
        return null;
    }

    void GenerateTerrain()
    {
        int vertCountX = gridSizeX + 1;
        int vertCountZ = gridSizeZ + 1;
        Vector3[,] gridVertices = new Vector3[vertCountX, vertCountZ];
        tileGrid = new TileData[gridSizeX, gridSizeZ];

        // Génération des sommets partagés
        for (int z = 0; z < vertCountZ; z++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                float y = Random.Range(-maxHeight, maxHeight);
                gridVertices[x, z] = new Vector3(x * tileSize, y, z * tileSize);
            }
        }

        // Création de chaque tuile comme GameObject
        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                Vector3 v1 = gridVertices[x, z];
                Vector3 v2 = gridVertices[x + 1, z];
                Vector3 v3 = gridVertices[x, z + 1];
                Vector3 v4 = gridVertices[x + 1, z + 1];

                Vector3 center = (v1 + v2 + v3 + v4) / 4f;

                GameObject tileGO = new GameObject($"Tile_{x}_{z}");
                tileGO.transform.position = center;
                tileGO.transform.SetParent(transform);

                MeshFilter mf = tileGO.AddComponent<MeshFilter>();
                MeshRenderer mr = tileGO.AddComponent<MeshRenderer>();
                MeshCollider mc = tileGO.AddComponent<MeshCollider>();

                // Construction du mesh relatif au centre
                Mesh mesh = new Mesh();
                mesh.vertices = new Vector3[] {
                v1 - center, // 0
                v2 - center, // 1
                v3 - center, // 2
                v4 - center  // 3
            };
                mesh.triangles = new int[] {
                0, 2, 1,
                1, 2, 3
            };
                mesh.RecalculateNormals();

                mf.mesh = mesh;
                mr.material = new Material(Shader.Find("Standard"));
                mc.sharedMesh = mesh;

                // Normale du plan formé par les 4 sommets (approximée via deux triangles)
                Vector3 normalA = Vector3.Cross(v2 - v1, v3 - v1);
                Vector3 normalB = Vector3.Cross(v4 - v2, v3 - v2);
                Vector3 normal = (normalA + normalB).normalized;

                if (Vector3.Dot(normal, Vector3.up) < 0)
                    normal = -normal;

                // Angle entre cette normale et la verticale
                float slope = Vector3.Angle(normal, Vector3.up); // 0° = plat, 90° = vertical
                float reliability = 1f - Mathf.Clamp01(slope / 45f);

                // Ajout du script TileInfo
                var info = tileGO.AddComponent<TileInfo>();
                info.slope = slope;
                info.reliability = reliability;

                // Stockage dans la grille
                TileData tile = new TileData
                {
                    center = center,
                    slope = slope,
                    reliability = reliability,
                    tileObject = tileGO
                };
                tileGrid[x, z] = tile;
            }
        }
    }

    public void Regenerate()
    {
        // Supprime les anciennes tuiles
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GenerateTerrain(); // Regénère le terrain manuellement
    }


}
