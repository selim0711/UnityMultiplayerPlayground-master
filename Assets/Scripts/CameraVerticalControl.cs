using Unity.Netcode;
using UnityEngine;

public class CameraVerticalControl : NetworkBehaviour
{
    /*
    public float verticalSensitivity = 100f;
    public float horizontalSensitivity = 100f;
    private float pitch = 0f;

    void Update()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
            return;

        HandleVerticalRotation();
        HandleHorizontalRotation();
    }

    void HandleVerticalRotation()
    {
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0f);
    }

    void HandleHorizontalRotation()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
            return;

        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        transform.parent.Rotate(Vector3.up * mouseX);
    } */
    public float verticalSensitivity = 100f;
    public float horizontalSensitivity = 100f;

    private float pitch = 0f; // Vertical rotation
    private float yaw = 0f;   // Horizontal rotation
    private Transform playerTransform;

    [SerializeField]
    private NetworkVariable<Vector2> playerRotation = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Start()
    {
        playerTransform = transform.parent; // The player object (parent of the camera)
    }

    void Update()
    {
        if (IsOwner)
        {
            // Immediate local rotation for the owner
            HandleRotation();
            UpdateNetworkRotation();
        }
        else
        {
            // Smooth synchronized rotation for non-owners
            ApplySynchronizedRotation();
        }
    }

    private void HandleRotation()
    {
        // Vertical rotation (up and down)
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Limit vertical view
        transform.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // Horizontal rotation (left and right)
        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        yaw += mouseX; // Accumulate yaw rotation
        playerTransform.localEulerAngles = new Vector3(0f, yaw, 0f); // Apply yaw rotation
    }

    private void UpdateNetworkRotation()
    {
        // Synchronisiere jede kleine Änderung sofort
        Vector2 newRotation = new Vector2(pitch, yaw);
        playerRotation.Value = newRotation;
    }

    private void ApplySynchronizedRotation()
    {
        // Smoothly apply the synchronized rotation for non-owners
        Vector2 targetRotation = playerRotation.Value;

        // Interpolate pitch (vertical) rotation
        pitch = Mathf.Lerp(pitch, targetRotation.x, Time.deltaTime * 20f); // Erhöhe den Faktor für schnellere Anpassung
        transform.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // Interpolate yaw (horizontal) rotation
        yaw = Mathf.Lerp(yaw, targetRotation.y, Time.deltaTime * 20f); // Erhöhe den Faktor für schnellere Anpassung
        playerTransform.localEulerAngles = new Vector3(0f, yaw, 0f);
    }
}
