using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Runtime.Serialization;
using System.Linq;
using Unity.VisualScripting;

public struct PlayerData
{
    public ulong playerID;
    public ulong entityID;
    public string playerName;
}

public struct PlayersData : INetworkSerializable
{
    public PlayerData[] playerData;

    public PlayersData(PlayerData[] playerData)
    {
        this.playerData = playerData;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if(serializer.IsReader)
        {
            int length = 0;

            serializer.SerializeValue(ref length);

            playerData = new PlayerData[length];

            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref playerData[i].playerID);
                serializer.SerializeValue(ref playerData[i].entityID);
                serializer.SerializeValue(ref playerData[i].playerName);
            }
        }
        else
        {
            int length = playerData?.Length ?? 0;

            serializer.SerializeValue(ref length);

            for (int i = 0; i < length; i++)
            {
                serializer.SerializeValue(ref playerData[i].playerID);
                serializer.SerializeValue(ref playerData[i].entityID);
                serializer.SerializeValue(ref playerData[i].playerName);
            }
        }
    }
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private Dictionary<ulong, string> clientUsernames = new Dictionary<ulong, string>();
    private List<PlayerBombHandler> playersInGame = new List<PlayerBombHandler>();

    private NetworkVariable<int> alivePlayersCount = new NetworkVariable<int>(0);

    [SerializeField]
    private GameObject bombPrefab; 

    [SerializeField]
    private GameObject playerPrefab = null;

    [SerializeField]
    private float countdownDuration = 3f; 

    [SerializeField]
    private Button startGameButton; 

    [SerializeField]
    private TMP_Text countdownText;

    
     private TMP_Text bombTimerText;

    private bool gameRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //Debug.Log("[GameManager] Initialized.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (startGameButton != null)
        {
           
            startGameButton.onClick.AddListener(OnStartGamePressed);
        }
    }
    private void OnStartGamePressed()
    {
        if (!IsServer) return;

        startGameButton.gameObject.SetActive(false);
        StartGameServerRpc();
    }

   


[Rpc(SendTo.Server)]
    public void SetLoggedInUsernameRpc(ulong clientId, string username)
    {
        var spawnedPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        var playerComp = spawnedPlayer.GetComponent<NetworkPlayer>();
        var netObj = playerComp.NetworkObject;

        
        netObj.SpawnAsPlayerObject(clientId);

        
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            client.PlayerObject = netObj;
        }

        
        if (!clientUsernames.ContainsKey(clientId))
        {
            Debug.Log($"[GameManager] Adding username for ClientId {clientId}: {username}");
            clientUsernames.Add(clientId, username);
        }
        else
        {
            Debug.Log($"[GameManager] Updating username for ClientId {clientId}: {username}");
            clientUsernames[clientId] = username;
        }

        
        Debug.Log($"Spawned player object with OwnerClientId: {netObj.OwnerClientId}");

        
        BroadcastUsernameToClientsServerRpc(clientId, netObj.NetworkObjectId, username);
    }
    public string GetUsernameForClient(ulong clientId)
    {
        if (clientUsernames.TryGetValue(clientId, out string username))
        {
            return username;
        }
        return "UNKNOWN";
    }

    [Rpc(SendTo.Server)]
    public void AskForAllUserDataServerRpc(RpcParams param = default)
    {
        List<PlayerData> playerDataList = new List<PlayerData>(NetworkPlayer.playerList.Count);


        foreach (KeyValuePair<ulong, NetworkPlayer> pair in NetworkPlayer.playerList)
        {
            PlayerData playerDatas = new PlayerData();

            playerDatas.playerID = pair.Value.playerID;
            playerDatas.entityID = pair.Value.NetworkObject.NetworkObjectId;
            playerDatas.playerName = pair.Value.GetPlayerName();

            playerDataList.Add(playerDatas);
        }

        SendAllUserDataToUserRpc(new PlayersData(playerDataList.ToArray()), RpcTarget.Single(param.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendAllUserDataToUserRpc(PlayersData data, RpcParams rpcParams)
    {
        for (int i = 0; i < data.playerData.Length; i++)
        {
            var p = GetNetworkObject(data.playerData[i].entityID);

            if (p)
            {
                var pNetComp = p.GetComponent<NetworkPlayer>();

                pNetComp.playerID = data.playerData[i].playerID;
                pNetComp.playerName = data.playerData[i].playerName;

                pNetComp.SetUIPlayerName();
            }
        }
    }
    public PlayerData GetPlayerData(ulong clientId)
    {
        
        if (clientUsernames.TryGetValue(clientId, out string username))
        {
            return new PlayerData { playerID = clientId, playerName = username };
        }
        return default; 
    }

    [ServerRpc]
    private void BroadcastUsernameToClientsServerRpc(ulong clientId, ulong entityID, string username)
    {
        //Debug.Log($"[GameManager] Broadcasting username '{username}' for Client {clientId}");

        var player = GetNetworkObject(entityID);

        if (player)
        {
            //Debug.Log("Found Player, giving ID");

            var playerComp = player.GetComponent<NetworkPlayer>();

            playerComp.SetOwnerIdClientRpc(clientId);

            playerComp.SetCameraRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));

            playerComp.UpdatePlayerNameClientRpc(username);
        }
        else
        {
           // Debug.Log("Couldnt Find Player!, not yet Spawned");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Start in: {i}";
            }
            yield return new WaitForSeconds(1);
        }

        if (countdownText != null)
        {
            countdownText.text = "";
        }

        AssignBombToRandomPlayer();
    }

    private void AssignBombToRandomPlayer()
    {
        playersInGame.Clear();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var handler = client.PlayerObject?.GetComponent<PlayerBombHandler>();
            if (handler != null && handler.IsAlive)
            {
                playersInGame.Add(handler);
            }
        }

        if (playersInGame.Count == 0)
        {
            Debug.LogError("No alive players available to assign the bomb.");
            return;
        }

        int randomIndex = Random.Range(0, playersInGame.Count);
        PlayerBombHandler selectedPlayer = playersInGame[randomIndex];
        //Debug.Log($"Assigning bomb to Player {selectedPlayer.OwnerClientId}.");

        var bombInstance = Instantiate(bombPrefab, selectedPlayer.transform.position, Quaternion.identity);
        bombInstance.GetComponent<NetworkObject>().Spawn(true);
        bombInstance.GetComponent<Bomb>().AssignOwner(selectedPlayer);
    }

    public void PlayerDied(PlayerBombHandler player)
    {
        playersInGame.Remove(player);
        player.OnPlayerDied();


        if (playersInGame.Count == 1)
        {
            gameRunning = false;
            var winner = playersInGame[0];
            string winnerName = GetUsernameForClient(winner.OwnerClientId); // Name vom Gewinner bekommen
            Debug.Log($"Game Over! Player {winnerName} wins!");

            // Alle spieler informieren wer der Gewinner ist
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerHandler = client.PlayerObject?.GetComponent<PlayerBombHandler>();
                if (playerHandler != null)
                {
                    playerHandler.ShowWinnerAnnouncementClientRpc(winnerName, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { client.ClientId }
                        }
                    });
                }
            }

            // Zeigt ob man Gewonnen/Verloren hat
            winner.ShowWinUIClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { winner.OwnerClientId }
                }
            });

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerHandler = client.PlayerObject?.GetComponent<PlayerBombHandler>();
                if (playerHandler != null && playerHandler.OwnerClientId != winner.OwnerClientId)
                {
                    playerHandler.ShowLoseUIClientRpc(new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { playerHandler.OwnerClientId }
                        }
                    });
                }
            }
        }
        else
        {
            AssignBombToRandomPlayer();
        }
    }



    public bool IsPlayerAlive(PlayerBombHandler player)
    {
        return playersInGame.Contains(player);
    }
}
