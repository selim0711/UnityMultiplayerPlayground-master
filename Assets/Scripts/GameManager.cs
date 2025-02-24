using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Runtime.Serialization;
using System.Linq;
using Unity.VisualScripting;
using System;

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

    private NetworkVariable<int> alivePlayersCount = new NetworkVariable<int>(0);

    [SerializeField]
    private NetworkVariable<float> remainingGameTime = new NetworkVariable<float>();

    [SerializeField]
    private NetworkVariable<int> networkCountdown = new NetworkVariable<int>();

    private Dictionary<ulong, int> playerScores = new Dictionary<ulong, int>();

    [SerializeField]
    private GameObject spawnArea;

    [SerializeField]
    private GameObject ballPrefab;
    private GameObject currentBallInstance;

    [SerializeField]
    private GameObject playerPrefab = null;

    [SerializeField]
    private float countdownDuration = 3f;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private TMP_Text countdownText;


    [SerializeField]
    private TMP_Text gameTimerText;
    [SerializeField]
    private TMP_Text gameTimerText3D;

    [SerializeField]
    public bool gameRunning = false;

    [SerializeField]
    private float gameDurationInMinutes = 5f;

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

    private void Start()
    {
        remainingGameTime.OnValueChanged += OnGameTimeChanged;
        networkCountdown.OnValueChanged += UpdateCountdownDisplay;

        if (startGameButton != null)
        {

            startGameButton.onClick.AddListener(OnStartGamePressed);
        }
    }

    private void UpdateCountdownDisplay(int oldValue, int newValue)
    {
        if (countdownText != null)
        {
            countdownText.text = newValue > 0 ? $"Start in: {newValue}" : "";
        }
    }

    private void OnGameTimeChanged(float oldTime, float newTime)
    {
        UpdateGameTimerUI(newTime);
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
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y, // Ensure this is correctly set based on the surface of the plane
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
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
        Quaternion spawnRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0); // Zufällige Y-Rotation

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

    public void SetGameDuration(float minutes)
    {
        gameDurationInMinutes = minutes;
        Debug.Log($"📢 Spielzeit auf {minutes} Minuten gesetzt!");
    }

    private IEnumerator GameTimer()
    {
        remainingGameTime.Value = gameDurationInMinutes * 60; // Set to 10 minutes
        while (remainingGameTime.Value > 0)
        {
            yield return new WaitForSeconds(1);
            remainingGameTime.Value--;
        }

        gameRunning = false;
        Debug.Log("❌ Spielzeit abgelaufen!");

        PostGameReset();
    }

    private void PostGameReset()
    {
        // Despawnen des Balls, wenn vorhanden
        if (currentBallInstance != null)
        {
            Destroy(currentBallInstance);
            currentBallInstance = null;
            Debug.Log("🏀 Ball despawned!");
        }

        // Setze den Spielstatus zurück und aktiviere den Start-Button
        gameRunning = false;
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(true);
        }

        // Weitere Resets können hier hinzugefügt werden, z.B. Reset der Spielerpunkte oder ähnliches
    }

    private void UpdateGameTimerUI(float time)
    {
        if (gameTimerText != null)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            gameTimerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            gameTimerText3D.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }
    }
    private IEnumerator StartCountdown()
    {
        int countdownTime = 3;  // Set this to your countdown duration
        while (countdownTime >= 0)
        {
            networkCountdown.Value = countdownTime--;
            yield return new WaitForSeconds(1);
        }
        // Trigger any other actions post-countdown
        gameRunning = true;
        StartCoroutine(GameTimer());
        SpawnBallAtRandomLocation();
    }


    private void SpawnBallAtRandomLocation()
    {
        Vector3 ballSpawnPosition = GetValidSpawnPosition(); // Verwenden der gleichen Methode, um eine gültige Position zu erhalten
        if (ballSpawnPosition != Vector3.zero)
        {
            float spawnHeight = 40f; // Höhe, aus der der Ball fallen soll
            ballSpawnPosition.y += spawnHeight; // Erhöhen der y-Koordinate um den spawnHeight
            currentBallInstance = Instantiate(ballPrefab, ballSpawnPosition, Quaternion.identity); // Erstellen des Balls an der modifizierten Position
            currentBallInstance.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("Failed to spawn the ball due to no valid position found.");
        }
    } 
}
