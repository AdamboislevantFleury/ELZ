using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeDetector : MonoBehaviour
{
    [Header("Cone Settings")]
    public float height = 200f;
    public float angle = 45f;
    public Material coneMaterial;
    public int resolution = 30;
    public KeyCode toggleKey = KeyCode.V;

    private GameObject coneVisual;
    private bool coneVisible = true;

    // Sauvegarde des couleurs d’origine PAR sous-mesh
    private Dictionary<Renderer, Color[]> originalSubmeshColors = new();

    void Start()
    {
        CreateConeVisual();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && coneVisual != null)
        {
            coneVisible = !coneVisible;
            coneVisual.SetActive(coneVisible);
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, height);
        HashSet<(Renderer, int)> affected = new(); // Pour détecter les sous-mesh encore dans le cône

        foreach (var col in nearby)
        {
            MeshFilter mf = col.GetComponent<MeshFilter>();
            Renderer rend = col.GetComponent<Renderer>();
            if (mf == null || rend == null || rend.materials.Length == 0)
                continue;

            Mesh mesh = mf.sharedMesh;
            if (mesh == null || mesh.subMeshCount == 0)
                continue;

            Material[] materials = rend.materials;

            // Sauvegarder les couleurs d’origine
            if (!originalSubmeshColors.ContainsKey(rend))
            {
                Color[] originalColors = new Color[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    originalColors[i] = materials[i].color;

                originalSubmeshColors[rend] = originalColors;
            }

            for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.GetTriangles(submeshIndex);

                if (triangles.Length == 0) continue;

                // Calcul du centroïde
                Vector3 centroid = Vector3.zero;
                foreach (int triIndex in triangles)
                    centroid += vertices[triIndex];
                centroid /= triangles.Length;

                centroid = col.transform.TransformPoint(centroid);

                // Test de cône
                Vector3 basePos = transform.position;
                Vector3 projected = new Vector3(basePos.x, centroid.y, basePos.z);
                float hDist = Vector3.Distance(projected, centroid);
                float vDist = Mathf.Abs(centroid.y - basePos.y);
                float angleTo = Mathf.Atan2(hDist, vDist) * Mathf.Rad2Deg;

                if (vDist <= height && angleTo <= angle)
                {
                    float heightAtPoint = vDist;
                    float maxRadius = Mathf.Tan(angle * Mathf.Deg2Rad) * heightAtPoint;
                    float t = Mathf.Clamp01(hDist / maxRadius);
                    Color c = Color.Lerp(Color.green, Color.red, t);

                    materials[submeshIndex].color = c;
                    affected.Add((rend, submeshIndex));
                }
                else if (originalSubmeshColors.ContainsKey(rend))
                {
                    // Restaure uniquement ce sous-mesh
                    Color[] origColors = originalSubmeshColors[rend];
                    if (submeshIndex < origColors.Length)
                        materials[submeshIndex].color = origColors[submeshIndex];
                }
            }

            rend.materials = materials;
        }

        // Restauration des sous-meshs sortis du cône
        foreach (var kvp in originalSubmeshColors)
        {
            Renderer rend = kvp.Key;
            Color[] originalColors = kvp.Value;
            Material[] materials = rend.materials;

            for (int i = 0; i < materials.Length && i < originalColors.Length; i++)
            {
                if (!affected.Contains((rend, i)))
                    materials[i].color = originalColors[i];
            }

            rend.materials = materials;
        }
    }

    // Génère le mesh visuel du cône
    Mesh GenerateConeMesh()
    {
        Mesh mesh = new();
        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();

        float radius = Mathf.Tan(angle * Mathf.Deg2Rad) * height;

        for (int i = 0; i <= resolution; i++)
        {
            float theta = 2 * Mathf.PI * i / resolution;
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            vertices.Add(new Vector3(x, -height, z));
        }

        for (int i = 1; i < resolution; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    void CreateConeVisual()
    {
        coneVisual = new GameObject("ConeVisual");
        coneVisual.transform.SetParent(transform);
        coneVisual.transform.localPosition = Vector3.zero;
        coneVisual.transform.localRotation = Quaternion.identity;

        MeshFilter mf = coneVisual.AddComponent<MeshFilter>();
        MeshRenderer mr = coneVisual.AddComponent<MeshRenderer>();

        mf.mesh = GenerateConeMesh();

        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(1f, 0f, 0f, 0.15f);

        mr.material = mat;
    }
}
