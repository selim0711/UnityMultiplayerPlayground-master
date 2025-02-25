using Unity.Netcode;
using UnityEngine;

public class SpeedItem : NetworkBehaviour
{
    public float boostDuration = 5.0f;
    public float speedMultiplier = 1.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerWithRaycastControl>();
            if (player != null && player.IsOwner && !player.isSpeedBoostActive)
            {
                player.ApplySpeedBoost(boostDuration, speedMultiplier);
                DespawnItem(); // Despawn oder deaktiviere das Item
            }
        }
    }

    private void DespawnItem()
    {
        if (IsServer) // Stelle sicher, dass nur der Server das Item deaktiviert
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(); // Entfernt das Item komplett aus der Szene
            }
        }
        else
        {
            gameObject.SetActive(false); // Deaktiviere das Item, falls nicht am Server
        }
    }
}
