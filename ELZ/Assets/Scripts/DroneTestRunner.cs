using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneTestRunner : MonoBehaviour
{
    [Header("Paramètres")]
    public DroneSpawner spawner;
    public int iterations = 10;

    private int currentIteration = 0;
    private bool runningRandom = false;

    private float totalReliabilityMost = 0f;
    private float totalReliabilityRandom = 0f;

    private int completedMost = 0;
    private int completedRandom = 0;

    void Start()
    {
        StartCoroutine(RunTests());
    }

    IEnumerator RunTests()
    {
        // Phase 1: drones avec SetMostReliableTargetTile
        spawner.useRandomTarget = false;

        for (int i = 0; i < iterations; i++)
        {
            yield return SpawnAndWaitForResult(isRandom: false);
        }

        // Phase 2: drones avec SetRandomTargetTile
        spawner.useRandomTarget = true;
        runningRandom = true;

        for (int i = 0; i < iterations; i++)
        {
            yield return SpawnAndWaitForResult(isRandom: true);
        }

        Debug.Log($"--- Résultats des tests sur {iterations} itérations ---");
        Debug.Log($"Moyenne (Most Reliable): {totalReliabilityMost / iterations * 100f:0.0}%");
        Debug.Log($"Moyenne (Random):        {totalReliabilityRandom / iterations * 100f:0.0}%");
    }

    IEnumerator SpawnAndWaitForResult(bool isRandom)
    {
        GameObject drone = spawner.SpawnSingleDrone(); // Fonction modifiée (voir plus bas)
        DroneController controller = drone.GetComponent<DroneController>();

        bool done = false;
        float reliabilityResult = 0f;

        controller.OnLanded = (reliability) =>
        {
            reliabilityResult = reliability;
            done = true;
        };

        while (!done)
            yield return null;

        if (isRandom)
        {
            totalReliabilityRandom += reliabilityResult;
            completedRandom++;
        }
        else
        {
            totalReliabilityMost += reliabilityResult;
            completedMost++;
        }
    }
}
