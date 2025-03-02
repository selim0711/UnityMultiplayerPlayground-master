using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerBallHandler : NetworkBehaviour
{
    NetworkPlayer player = null;

    [SerializeField]
    private float ballTransferRadius = 2.0f;

    [SerializeField]
    private float ballCooldown = 1.0f;

    private bool hasBall = false;
    private bool canTransferBall = true;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        player = GetComponent<NetworkPlayer>();
    }

    private void Update()
    {
        if (!IsServer || !hasBall) return;

        CheckForNearbyPlayers();
    }

    private void CheckForNearbyPlayers()
    {
        if (!canTransferBall) return;

        Collider[] nearbyPlayers = Physics.OverlapSphere(transform.position, ballTransferRadius);
        foreach (var collider in nearbyPlayers)
        {
            var otherPlayer = collider.GetComponent<PlayerBallHandler>();
            if (otherPlayer != null && otherPlayer != this &&  !otherPlayer.hasBall)
            {
                PlayerData pd = GameManager.Instance.GetPlayerData(otherPlayer.OwnerClientId);
                //Debug.Log($"Found player {pd.playerName} with ID {pd.playerID}");
                TransferBallServerRpc(otherPlayer.OwnerClientId);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransferBallServerRpc(ulong targetPlayerId)
    {
        var targetPlayer = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetPlayerId]
            .GetComponent<PlayerBallHandler>();

        if (targetPlayer != null && !targetPlayer.hasBall)
        {
            targetPlayer.ReceiveBall();
            RemoveBall();
            StartCoroutine(BallCooldown());
        }
    }

    private IEnumerator BallCooldown()
    {
        canTransferBall = false;
        yield return new WaitForSeconds(ballCooldown);
        canTransferBall = true;
    }

    public void ReceiveBall()
    {
        hasBall = true;
    }

    private void RemoveBall()
    {
        hasBall = false;
    }


    public ulong GetPlayerID()
    {
        return player.playerID;
    }
}
