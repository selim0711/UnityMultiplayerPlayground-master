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
    private GameObject spawnArea;

    [SerializeField]
    private GameObject ballPrefab; 

    [SerializeField]
    private GameObject playerPrefab = null;

    [SerializeField]
    private float countdownDuration = 3f; 

    [SerializeField]
    private Button startGameButton; 

    [SerializeField]
    private TMP_Text countdownText;


    
     private TMP_Text gameTimerText;

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

    private Vector3 GetValidSpawnPosition()
    {
        if (!spawnArea)
        {
            Debug.LogError("Spawn area GameObject is not set.");
            return Vector3.zero;
        }

        Collider spawnAreaCollider = spawnArea.GetComponent<Collider>();
        if (!spawnAreaCollider)
        {
            Debug.LogError("Spawn area GameObject does not have a Collider component.");
            return Vector3.zero;
        }

        Bounds bounds = spawnAreaCollider.bounds;
        Debug.Log($"Bounds: {bounds}"); // Log the actual bounds to verify their values

        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y, // Ensure this is correctly set based on the surface of the plane
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (!Physics.CheckSphere(randomPosition, 1f, LayerMask.GetMask("Player", "Obstacle"), QueryTriggerInteraction.Ignore))
            {
                Debug.Log("Valid position found: " + randomPosition);
                return randomPosition;
            }
        }

        Debug.LogError("Failed to find a valid position after " + maxAttempts + " attempts.");
        return Vector3.zero;
    }


    [Rpc(SendTo.Server)]
    public void SetLoggedInUsernameRpc(ulong clientId, string username)
    {
        Vector3 spawnPosition = GetValidSpawnPosition();
        Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0); // Zufällige Y-Rotation

        var spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);

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

        SpawnBall();
    }

    private void SpawnBall()
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

        var ballInstance = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity); // Spawne den Ball im Zentrum
        ballInstance.GetComponent<NetworkObject>().Spawn();
        ballInstance.GetComponent<Ball>();
    }
}
