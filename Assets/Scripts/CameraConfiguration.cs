using Unity.Netcode;
using UnityEngine;

public class CameraConfiguration : MonoBehaviour
{
    private Camera mainCamera; // Zugriff auf die eigentliche Kamera

    void Start()
    {
        // Sicherstellen, dass das Skript nur f�r den lokalen Spieler ausgef�hrt wird
        var networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject == null || !networkObject.IsOwner)
        {
            enabled = false; // Deaktiviert das Skript f�r andere Clients
            return;
        }

        // Kamera finden (wichtig f�r Cinemachine-Integration)
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
