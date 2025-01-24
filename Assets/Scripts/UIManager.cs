using DilmerGames.Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private Button startServerButton;

    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private TextMeshProUGUI playersInGameText;

    [SerializeField]
    private TMP_InputField joinCodeInput;

    [SerializeField]
    private Button executePhysicsButton;


    [SerializeField] private Slider staminaSliderPrefab;
    [SerializeField] private Transform uiParent;




    private bool hasServerStarted;


    public Slider CreateStaminaSliderForPlayer(ulong clientId)
    {
        Slider newSlider = Instantiate(staminaSliderPrefab, uiParent);
        newSlider.name = $"StaminaSlider_{clientId}";
        return newSlider;
    }


    private void Awake()
    {
        Cursor.visible = true;
    }
    /*

void Update()
{
        playersInGameText.text = $"Players in game: {GameManager.Instance.GetAlivePlayersCount()}";
    }*/
    void Update()
    {
        if (PlayersManager.Instance != null)
        {
            playersInGameText.text = $"Players in game: {NetworkPlayer.playerList.Count}";
        }
        else
        {
            playersInGameText.text = "PlayersManager instance not found.";
        }
        
    }
    void Start()
    {
        // START SERVER
        startServerButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
            {
                Logger.Instance.LogInfo("Server started...");
                SetupHostOrServerCallbacks(); 
            }
            else
            {
                Logger.Instance.LogInfo("Unable to start server...");
            }
        });

        startHostButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Logger.Instance.LogInfo("Host started...");

                // Abrufen der DBConnection-Instanz
                DBConnection dbConnection = FindObjectOfType<DBConnection>();
                if (dbConnection != null)
                {
                    // Benutzernamen abrufen und erneut setzen
                    string username = dbConnection.GetUsername();
                    ulong localClientId = NetworkManager.Singleton.LocalClientId;
                    GameManager.Instance.SetLoggedInUsernameRpc(localClientId, username);
                }
                else
                {
                    Logger.Instance.LogWarning("DBConnection instance not found!");
                }
            }
            else
            {
                Logger.Instance.LogInfo("Unable to start host...");
            }
        });

        // START CLIENT
        startClientButton?.onClick.AddListener(async () =>
        {
            if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCodeInput.text))
                await RelayManager.Instance.JoinRelay(joinCodeInput.text);

            if (NetworkManager.Singleton.StartClient())
            {
                Logger.Instance.LogInfo("Client started...");
                SetupClientCallbacks(); // Setup callbacks for client-related events
            }
            else
            {
                Logger.Instance.LogInfo("Unable to start client...");
            }
        });

        // STATUS TYPE CALLBACKS
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Logger.Instance.LogInfo($"{id} just connected...");
        };

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            hasServerStarted = true;
        };


    }



    private void SetupHostOrServerCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Logger.Instance.LogInfo($"[Server] Client {clientId} just connected.");

            // If the host is the server, log the host's `ClientId`
            if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Logger.Instance.LogInfo($"[Host] Host ClientId: {clientId}");
            }
        };
    }

    private void SetupClientCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            GameManager.Instance.SetLoggedInUsernameRpc(clientId, DBConnection.usernameAH);

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Logger.Instance.LogInfo($"[Client] Connected to server. Assigned ClientId: {clientId}");
            }
        };
    }
    private void AssignHostUsername()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            string hostUsername = "HostUsername"; // Replace with actual input
            GameManager.Instance.SetLoggedInUsernameRpc(hostClientId, hostUsername);
            Logger.Instance.LogInfo($"[Host] Assigned username '{hostUsername}' to ClientId {hostClientId}");
        }
    }
}
