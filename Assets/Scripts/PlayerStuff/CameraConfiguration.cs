using Unity.Netcode;
using UnityEngine;

public class CameraConfiguration : MonoBehaviour
{
    private Camera mainCamera; // Zugriff auf die eigentliche Kamera

    void Start()
    {
        // Sicherstellen, dass das Skript nur für den lokalen Spieler ausgeführt wird
        var networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject == null || !networkObject.IsOwner)
        {
            enabled = false; // Deaktiviert das Skript für andere Clients
            return;
        }

        // Kamera finden (wichtig für Cinemachine-Integration)
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Keine Kamera auf diesem GameObject gefunden!");
            return;
        }

        // LocalPlayer-Layer ignorieren
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("LocalPlayer"));
    }
}
