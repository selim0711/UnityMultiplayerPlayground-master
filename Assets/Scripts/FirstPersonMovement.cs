using Cinemachine;
using Unity.Netcode;
using UnityEngine;
public class FirstPersonMovement : NetworkBehaviour
{
    public float movementSpeed = 5.0f;
    public float mouseSensitivity = 2.0f;
    public Camera playerCamera;

    private float verticalRotation = 0;
    public float upDownRange = 60.0f;

    private CharacterController characterController;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

   
    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor when playing
        Cursor.lockState = CursorLockMode.Locked;

        if (IsOwner)
        {
            // Finde die Cinemachine Virtual Camera in der Szene
            if (virtualCamera == null)
            {
                virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }

            if (virtualCamera != null)
            {
                // Setze diese Kamera, um diesem Spieler zu folgen und auf ihn zu schauen
                virtualCamera.Follow = transform;
                virtualCamera.LookAt = transform;
            }
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    void HandleMovement()
    {
        float forwardSpeed = Input.GetAxis("Vertical") * movementSpeed;
        float sideSpeed = Input.GetAxis("Horizontal") * movementSpeed;

        Vector3 speed = new Vector3(sideSpeed, 0, forwardSpeed);
        speed = transform.rotation * speed;

        characterController.SimpleMove(speed);
    }

    void HandleRotation()
    {
        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        // Begrenzung der vertikalen Kameraneigung
        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, playerCamera.transform.localEulerAngles.y, 0);
    }
}
