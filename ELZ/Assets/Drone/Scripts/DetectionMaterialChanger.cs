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

        Collider[] nearby = Physics.OverlapSphere(transform.position, height);
        var affected = new HashSet<Renderer>();
        float weatherFactor = weatherManager != null ? weatherManager.GetWeatherReliability() : 1f;

        foreach (var col in nearby)
        {
            var mf = col.GetComponent<MeshFilter>();
            var rend = col.GetComponent<Renderer>();
            if (mf == null || rend == null || rend.material == null) continue;

            if (!originalColors.ContainsKey(rend))
                originalColors[rend] = rend.material.color;

            var vertices = mf.sharedMesh.vertices;
            if (vertices == null || vertices.Length == 0) continue;

            Vector3 centroid = Vector3.zero;
            foreach (var v in vertices)
                centroid += col.transform.TransformPoint(v);
            centroid /= vertices.Length;

            float vDist = Mathf.Abs(centroid.y - transform.position.y);
            if (vDist > height) { RestoreRenderer(rend); continue; }

            Vector3 proj = new Vector3(transform.position.x, centroid.y, transform.position.z);
            float hDist = Vector3.Distance(proj, centroid);
            float maxRadius = Mathf.Tan(angle * Mathf.Deg2Rad) * vDist;
            if (hDist > maxRadius) { RestoreRenderer(rend); continue; }

            float lateral = 1f - Mathf.Clamp01(hDist / maxRadius);
            float slopeDeg = Vector3.Angle(col.transform.up, Vector3.up);
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

        foreach (var kvp in originalColors)
        {
            var rend = kvp.Key;
            if (!affected.Contains(rend)) RestoreRenderer(rend);
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
