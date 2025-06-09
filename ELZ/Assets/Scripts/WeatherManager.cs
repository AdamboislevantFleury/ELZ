using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Weather Conditions (0 to 1)")]
    [Range(0f, 1f)] public float rainIntensity = 0f;
    [Range(0f, 1f)] public float fogDensity = 0f;
    [Range(0f, 1f)] public float snowIntensity = 0f;

    [Header("Effects Multipliers")]
    [Range(0f, 1f)] public float rainImpact = 0.4f; // fiabilité max = 60%
    [Range(0f, 1f)] public float fogImpact = 0.5f;  // fiabilité max = 50%
    [Range(0f, 1f)] public float snowImpact = 0.7f; // fiabilité max = 30%

    void Update()
    {
        RenderSettings.fog = fogDensity > 0f;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity; // Capacité Unity :contentReference[oaicite:2]{index=2}
    }

    public float GetWeatherReliability()
    {
        float rFactor = Mathf.Lerp(1f, rainImpact, rainIntensity);
        float fFactor = Mathf.Lerp(1f, fogImpact, fogDensity);
        float sFactor = Mathf.Lerp(1f, snowImpact, snowIntensity);
        return rFactor * fFactor * sFactor;
    }
}
