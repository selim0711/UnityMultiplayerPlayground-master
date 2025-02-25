using Unity.Netcode;
using UnityEngine;

public class JumpItem : NetworkBehaviour
{
    public float boostDuration = 5f;  // Dauer des Jump Boosts
    public float jumpMultiplier = 2f; // Multiplikator für die Sprunghöhe

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerWithRaycastControl>();
            if (player != null && player.IsOwner && !player.isJumpBoostActive)
            {
                player.ApplyJumpBoost(boostDuration, jumpMultiplier);
                DespawnItem(); // Deaktiviert das Item
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
