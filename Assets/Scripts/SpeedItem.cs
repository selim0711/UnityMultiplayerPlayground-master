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
                DespawnItem();
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
