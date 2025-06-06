using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Tooltip("Liste des caméras à alterner.")]
    public List<Camera> cameras;

    [Tooltip("Touche pour changer de caméra.")]
    public KeyCode switchKey = KeyCode.C;

    private int currentCameraIndex = 0;

    void Start()
    {
        if (cameras == null || cameras.Count == 0)
        {
            Debug.LogWarning("CameraSwitcher : Aucune caméra définie dans la liste.");
            return;
        }

        // Désactiver toutes les caméras sauf la première
        for (int i = 0; i < cameras.Count; i++)
        {
            cameras[i].enabled = (i == 0);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SwitchToNextCamera();
        }
    }

    private void SwitchToNextCamera()
    {
        if (cameras == null || cameras.Count == 0)
            return;

        // Désactiver la caméra actuelle
        cameras[currentCameraIndex].enabled = false;

        // Passer à la suivante
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

        // Activer la nouvelle caméra
        cameras[currentCameraIndex].enabled = true;

        Debug.Log("Caméra active : " + cameras[currentCameraIndex].name);
    }
}