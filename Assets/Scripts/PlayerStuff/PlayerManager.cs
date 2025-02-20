using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

   public void UpdatePlayerName(ulong clientId, string newName)
{
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject netObject = client.PlayerObject;
            if (netObject != null)
            {
                NetworkPlayer playerScript = netObject.GetComponent<NetworkPlayer>();;
            }
        }
    }

}
