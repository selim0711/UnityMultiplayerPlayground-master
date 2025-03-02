using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    private NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();
    private Transform followTarget; 
    private Vector3 localPosition; 

    public bool CanInteract(ulong clientId)
    {
        return ownerClientId.Value == 0 || ownerClientId.Value == clientId;
    }

    [Rpc(SendTo.Server)]
    public void InteractWithBallRpc(ulong clientId)
    {
        ownerClientId.Value = clientId;
    }

    [Rpc(SendTo.Server)]
    public void ClearOwnerRpc()
    {
        ownerClientId.Value = 0; 
        followTarget = null; 
        gameObject.GetComponent<Rigidbody>().isKinematic = false; 
    }

    public void SetOwner(Transform playerTransform, Transform hand)
    {
        followTarget = hand;
        localPosition = Vector3.zero;
        gameObject.GetComponent<Rigidbody>().isKinematic = true; ;
    }

    void Update()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.TransformPoint(localPosition);
        }
    }
}
