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
    public float minDetectionDistance = 2f;

    [Header("Memory")]
    public float memoryDuration = 0.3f;

    private GameObject coneVisual;
    private bool coneVisible = true;

    public WeatherManager weatherManager;

    private Dictionary<Renderer, Color> originalColors = new();
    private Material coneMaterialInstance;
    
    public WindManager windManager;

    private class DetectionState
    {
        public float lastSeenTime;
        public GameObject label;
    }

    private Dictionary<Renderer, DetectionState> detectionMemory = new();

    void Start()
    {
        CreateConeVisual();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && coneVisual != null)
            coneVisual.SetActive(!coneVisual.activeSelf);

        float weatherFactor = weatherManager != null ? weatherManager.GetWeatherReliability() : 1f;
        Vector3 origin = transform.position;
        int rayCount = 200;
        float currentTime = Time.time;

        HashSet<Renderer> seenThisFrame = new();

        for (int i = 0; i < rayCount; i++)
        {
            float theta = Random.Range(0f, 2 * Mathf.PI);
            float phi = Random.Range(0f, angle * Mathf.Deg2Rad);

            Vector3 dirLocal = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                -Mathf.Cos(phi),
                Mathf.Sin(phi) * Mathf.Sin(theta)
            );
            Vector3 direction = transform.TransformDirection(dirLocal);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, height))
            {
                if (Vector3.Distance(origin, hit.point) < minDetectionDistance)
                    continue;

                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend == null || rend.material == null) continue;

                TileInfo info = hit.collider.GetComponent<TileInfo>();
                if (info == null) continue;

                float slopeDeg = info.slope;
                float orientationReliability = info.reliability;

                Vector3 centroid = hit.collider.bounds.center;
                float verticalDist = Mathf.Abs(centroid.y - origin.y);
                float radiusAtPoint = Mathf.Tan(angle * Mathf.Deg2Rad) * verticalDist;
                float horizontalDist = Vector3.Distance(new Vector3(origin.x, centroid.y, origin.z), centroid);
                if (horizontalDist > radiusAtPoint) continue;

                float reliability;
                Color c;

                if (slopeDeg >= 20f)
                {
                    reliability = 0f;
                    c = Color.black;
                }
                else
                {
                    // Linear reliability between 0° (100%) and 20° (0%)
                    float slopeReliability = 1f - Mathf.Clamp01(slopeDeg / 20f);
                    reliability = slopeReliability * weatherFactor;
                    Color green = Color.green;
                    Color orange = new Color(1f, 0.65f, 0f); // RGB pour orange
                    Color red = Color.red;

                    if (slopeReliability > 0.5f)
                    {
                        float t = (slopeReliability - 0.5f) * 2f; // Remap [0.5, 1] → [0, 1]
                        c = Color.Lerp(orange, green, t);
                    }
                    else
                    {
                        float t = slopeReliability * 2f; // Remap [0, 0.5] → [0, 1]
                        c = Color.Lerp(red, orange, t);
                    }

                }

                // Ancien calcul basé aussi sur la distance :
                // float lateral = 1f - Mathf.Clamp01(horizontalDist / radiusAtPoint);
                // float idxBase = lateral * orientationReliability;
                // float reliability = idxBase * weatherFactor;

                if (!originalColors.ContainsKey(rend))
                    originalColors[rend] = rend.material.color;

                rend.material.color = c;
                seenThisFrame.Add(rend);

                if (!detectionMemory.ContainsKey(rend))
                {
                    GameObject label = new GameObject("ReliabilityLabel");
                    var text = label.AddComponent<TextMesh>();
                    text.fontSize = 32;
                    text.characterSize = 0.1f;
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.white;

                    detectionMemory[rend] = new DetectionState
                    {
                        lastSeenTime = currentTime,
                        label = label
                    };
                }
                else
                {
                    detectionMemory[rend].lastSeenTime = currentTime;
                }
            }
        }

        List<Renderer> toForget = new();
        foreach (var kvp in detectionMemory)
        {
            Renderer rend = kvp.Key;
            DetectionState state = kvp.Value;

            if (!seenThisFrame.Contains(rend))
            {
                if (Time.time - state.lastSeenTime > memoryDuration)
                {
                    if (originalColors.ContainsKey(rend))
                        rend.material.color = originalColors[rend];

                    if (state.label != null)
                        state.label.SetActive(false);

                    toForget.Add(rend);
                }
            }
        }

        foreach (var rend in toForget)
        {
            detectionMemory.Remove(rend);
        }
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
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(1f, 0f, 0f, 0.30f); // Rouge translucide
        

        mr.material = mat;
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
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }
}
