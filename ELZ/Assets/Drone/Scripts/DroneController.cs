using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DroneController : MonoBehaviour
{
    [Header("Contrôles personnalisables")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.LeftControl;

    [Header("Paramètres de déplacement")]
    public float moveSpeed = 200f;

    [Header("Rotation à la souris")]
    public float mouseSensitivity = 2f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("Caméra fixée au drone")]
    public Transform cameraTransform;
    public Vector3 cameraOffset = new Vector3(0, 1.5f, -4f);

    private CharacterController controller;
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Verrouille le curseur au centre
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        UpdateCameraPosition();

        // Appuyer sur Échap pour libérer la souris
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(forwardKey))
            direction += transform.forward;
        if (Input.GetKey(backwardKey))
            direction -= transform.forward;
        if (Input.GetKey(leftKey))
            direction -= transform.right;
        if (Input.GetKey(rightKey))
            direction += transform.right;
        if (Input.GetKey(upKey))
            direction += transform.up;
        if (Input.GetKey(downKey))
            direction -= transform.up;

        controller.Move(direction.normalized * moveSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Appliquer la rotation horizontale (yaw) au drone
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Appliquer la rotation verticale (pitch) à la caméra
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }


    void UpdateCameraPosition()
    {
        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + transform.TransformVector(cameraOffset);
        }
    }
}
