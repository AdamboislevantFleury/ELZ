using UnityEngine;

public class WindManager : MonoBehaviour
{
    public Vector3 windDirection = Vector3.zero;
    public float windStrength = 0f;

    public Vector3 GetWindDirection()
    {
        return windDirection.normalized;
    }

    public float GetWindStrength()
    {
        return windStrength;
    }
}
