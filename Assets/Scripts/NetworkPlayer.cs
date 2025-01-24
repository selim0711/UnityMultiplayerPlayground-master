using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
public class NetworkPlayer : NetworkBehaviour
{
    static public Dictionary<ulong, NetworkPlayer> playerList = new Dictionary<ulong, NetworkPlayer>();

    

    [SerializeField] private TMP_Text playerInfoText;

    public string playerName = "";
    public ulong playerID = 0;



    public override void OnNetworkSpawn()
    {
        playerList.Add(playerID, this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        playerList.Remove(playerID);
    }

    

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetUIPlayerName()
    {
        if (playerInfoText != null)
        {
            playerInfoText.text = playerName;
        }
        else
        {
            //Debug.LogError("[NetworkPlayer] PlayerInfoText not assigned in inspector!");
        }
    }

    [ClientRpc]
    public void SetOwnerIdClientRpc(ulong playerID)
    {
        this.playerID = playerID;

        if(!playerList.ContainsKey(playerID))
            playerList.Add(playerID, this);


        GameManager.Instance.AskForAllUserDataServerRpc();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SetCameraRpc(RpcParams rpcParams)
    {
        PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
    }

    [ClientRpc]
    public void UpdatePlayerNameClientRpc(string newName)
    {
        playerName = newName;

        SetUIPlayerName();
    }
}
