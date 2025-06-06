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

    private Dictionary<Renderer, Color> originalColors = new();

    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            Material mat = renderer.material;

            // Utiliser le shader standard avec mode Transparent
            mat.shader = Shader.Find("Standard");
            mat.SetFloat("_Mode", 3); // Mode Transparent

            // Activer les bons réglages pour la transparence
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Couleur avec transparence (RGBA) — ici, rouge translucide
            mat.color = new Color(1f, 0f, 0f, 0.1f); // alpha = 0.1 → presque invisible
        }
    }


    void Update()
    {
        Collider[] all = Physics.OverlapSphere(transform.position, height);
        List<Renderer> inCone = new();

        foreach (var col in all)
        {
            Vector3 objectPos = col.transform.position;
            Vector3 basePos = transform.position;
            Vector3 projected = new Vector3(basePos.x, objectPos.y, basePos.z); // projection on vertical line

            float horizontalDistance = Vector3.Distance(projected, objectPos);
            float verticalDistance = Mathf.Abs(objectPos.y - basePos.y);
            float angleTo = Mathf.Atan2(horizontalDistance, verticalDistance) * Mathf.Rad2Deg;


            if (verticalDistance <= height && angleTo <= angle)
            {
                Renderer rend = col.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (!originalColors.ContainsKey(rend))
                        originalColors[rend] = rend.material.color;

// Position projetée de l'objet sur l'axe vertical
                    projected = new Vector3(transform.position.x, col.transform.position.y, transform.position.z);

// Distance horizontale réelle entre l'objet et l'axe
                    horizontalDistance = Vector3.Distance(projected, col.transform.position);

// À la hauteur Y actuelle de l’objet, on calcule le rayon du cône
                    float heightAtPoint = Mathf.Abs(col.transform.position.y - transform.position.y);
                    float maxRadiusAtHeight = Mathf.Tan(angle * Mathf.Deg2Rad) * heightAtPoint;

// Interpolation entre vert (centre) et rouge (bord)
                    float t = Mathf.Clamp01(horizontalDistance / maxRadiusAtHeight);
                    rend.material.color = Color.Lerp(Color.green, Color.red, t);

                    inCone.Add(rend);
                }
            }
        }

        foreach (var kvp in new List<Renderer>(originalColors.Keys))
        {
            if (!inCone.Contains(kvp))
            {
                kvp.material.color = originalColors[kvp];
                originalColors.Remove(kvp);
            }
        }
    }

    Mesh GenerateConeMesh()
    {
        Mesh mesh = new();
        List<Vector3> vertices = new() { Vector3.zero }; // Tip of the cone
        List<int> triangles = new();

        float radius = Mathf.Tan(angle * Mathf.Deg2Rad) * height;

        for (int i = 0; i <= resolution; i++)
        {
            float theta = 2 * Mathf.PI * i / resolution;
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            vertices.Add(new Vector3(x, -height, z));
        }

        for (int i = 1; i <= resolution; i++)
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
}
