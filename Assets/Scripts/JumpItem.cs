using Unity.Netcode;
using UnityEngine;

public class JumpItem : NetworkBehaviour
{
    public float boostDuration = 5f; 
    public float jumpMultiplier = 2f; 

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
        if (IsServer)
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn();
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
