using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    private NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();
    private Transform followTarget; // The target to follow (player's hand)
    private Vector3 localPosition; // Local position relative to the player's hand

    public bool CanInteract(ulong clientId)
    {
        return ownerClientId.Value == 0 || ownerClientId.Value == clientId;
    }

    [Rpc(SendTo.Server)]
    public void InteractWithBallRpc(ulong clientId)
    {
        ownerClientId.Value = clientId; // Set new owner
    }

    [Rpc(SendTo.Server)]
    public void ClearOwnerRpc()
    {
        ownerClientId.Value = 0; // No owner
        followTarget = null; // Stop following
        gameObject.GetComponent<Rigidbody>().isKinematic = false; // Make the ball responsive to physics again
    }

    public void SetOwner(Transform playerTransform, Transform hand)
    {
        followTarget = hand;
        localPosition = Vector3.zero; // You can adjust this position
        gameObject.GetComponent<Rigidbody>().isKinematic = true; // Make the ball non-responsive to physics
    }

    void Update()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.TransformPoint(localPosition);
        }
    }
}
