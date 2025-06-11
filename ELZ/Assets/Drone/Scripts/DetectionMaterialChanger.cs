using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeDetector : MonoBehaviour
{
    [Header("Cone Settings")]
    public float height = 200f;
    public float angle = 45f;
    public int resolution = 30;
    public KeyCode toggleKey = KeyCode.V;
    
    [Header("Distance Settings")]
    public float minDetectionDistance = 2f; // Distance minimale depuis la base

    private GameObject coneVisual;
    private bool coneVisible = true;

    private Dictionary<Renderer, Color> originalColors = new();
    private Dictionary<Renderer, GameObject> reliabilityLabels = new();

    public WeatherManager weatherManager;

    void Start()
    {
        CreateConeVisual();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && coneVisual != null)
            coneVisual.SetActive(!coneVisual.activeSelf);

        float weatherFactor = weatherManager != null ? weatherManager.GetWeatherReliability() : 1f;

        var affected = new HashSet<Renderer>();
        Vector3 origin = transform.position;
        float minDistance = 2f; // Distance minimale à partir du sommet pour ignorer les objets trop proches
        int rayCount = 200;     // Plus élevé = plus de précision

        for (int i = 0; i < rayCount; i++)
        {
            float theta = Random.Range(0f, 2 * Mathf.PI);
            float phi = Random.Range(0f, angle * Mathf.Deg2Rad); // Limité à l'angle du cône

            // Sphère vers direction du cône
            Vector3 dirLocal = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                -Mathf.Cos(phi), // -Y car le cône pointe vers le bas
                Mathf.Sin(phi) * Mathf.Sin(theta)
            );

            Vector3 direction = transform.TransformDirection(dirLocal);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, height))
            {
                if (Vector3.Distance(origin, hit.point) < minDistance)
                    continue;

                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend == null || rend.material == null) continue;

                if (!originalColors.ContainsKey(rend))
                    originalColors[rend] = rend.material.color;

                // Position et pente
                Vector3 centroid = hit.collider.bounds.center;
                float verticalDist = Mathf.Abs(centroid.y - origin.y);
                float radiusAtPoint = Mathf.Tan(angle * Mathf.Deg2Rad) * verticalDist;
                float horizontalDist = Vector3.Distance(new Vector3(origin.x, centroid.y, origin.z), centroid);

                if (horizontalDist > radiusAtPoint) continue;

                float lateral = 1f - Mathf.Clamp01(horizontalDist / radiusAtPoint);
                float slopeDeg = Vector3.Angle(hit.collider.transform.up, Vector3.up);
                float orientationReliability = 1f - Mathf.Clamp01(slopeDeg / angle);
                float idxBase = orientationReliability > 0f ? lateral * orientationReliability : 0f;

                float reliabilityIndex = idxBase * weatherFactor;

                Color c = reliabilityIndex > 0f
                    ? Color.Lerp(Color.red, Color.green, reliabilityIndex)
                    : Color.black;

                rend.material.color = c;
                affected.Add(rend);

                if (!reliabilityLabels.ContainsKey(rend))
                {
                    GameObject label = new GameObject("ReliabilityLabel");
                    var text = label.AddComponent<TextMesh>();
                    text.fontSize = 32; text.characterSize = 0.1f;
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.white;
                    reliabilityLabels[rend] = label;
                }

                var labelGO = reliabilityLabels[rend];
                labelGO.transform.position = centroid + Vector3.up * 2f;
                labelGO.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                labelGO.GetComponent<TextMesh>().text =
                    $"Fiab: {(reliabilityIndex * 100f):0}%\nSlope: {slopeDeg:0.0}°\nMétéo : {(weatherFactor * 100f):0}%";
                labelGO.SetActive(true);
            }
        }

        // Restaurer les objets non touchés
        foreach (var kvp in originalColors)
        {
            var rend = kvp.Key;
            if (!affected.Contains(rend))
            {
                rend.material.color = kvp.Value;
                if (reliabilityLabels.ContainsKey(rend))
                    reliabilityLabels[rend].SetActive(false);
            }
        }
    }

    void RestoreRenderer(Renderer rend)
    {
        rend.material.color = originalColors[rend];
        if (reliabilityLabels.ContainsKey(rend))
            reliabilityLabels[rend].SetActive(false);
    }

    Mesh GenerateConeMesh()
    {
        Mesh mesh = new();
        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();
        float radius = Mathf.Tan(angle * Mathf.Deg2Rad) * height;

        for (int i = 0; i <= resolution; i++)
        {
            float theta = 2 * Mathf.PI * i / resolution;
            vertices.Add(new Vector3(radius * Mathf.Cos(theta), -height, radius * Mathf.Sin(theta)));
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

        var mf = coneVisual.AddComponent<MeshFilter>();
        var mr = coneVisual.AddComponent<MeshRenderer>();
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
