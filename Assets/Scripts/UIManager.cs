using DilmerGames.Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using Unity.Netcode.Transports.UTP;
using System.Data.Common;
using System.Collections;

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



    [SerializeField]
    private TMP_InputField ipInputField;  // Referenz zum IP-Adresse Eingabefeld
    [SerializeField]
    private Button connectButton;  // Verbindungsbutton


    private bool hasServerStarted;

    [SerializeField]
    private GameObject[] uiElementsToDeactivateOnHost;
    [SerializeField]
    private GameObject[] uiElementsToDeactivateOnJoin;
    private void DeactivateUIElements(GameObject[] elements)
    {
        foreach (GameObject element in elements)
        {
            element.SetActive(false);
        }
    }
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

    string GetLocalIPAddress()
    {
        string localIP = "";
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
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

        // START HOST
        startHostButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                string localIP = GetLocalIPAddress();
                Logger.Instance.LogInfo($"Host started... Local IP Address: {localIP}");
                DeactivateUIElements(uiElementsToDeactivateOnHost);

                // Display the IP address on UI or any relevant component
                // Update any relevant UI component to show the IP address to users

                DBConnection dbConnection = FindObjectOfType<DBConnection>();
                if (dbConnection != null)
                {
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
            {
                await RelayManager.Instance.JoinRelay(joinCodeInput.text);
            }

            if (NetworkManager.Singleton.StartClient())
            {
                Logger.Instance.LogInfo("Client started...");
                SetupClientCallbacks(); // Setup callbacks for client-related events
                DeactivateUIElements(uiElementsToDeactivateOnJoin);
            }
            else
            {
                Logger.Instance.LogInfo("Unable to start client...");
            }
        });

        // CONNECT BUTTON FOR DIRECT IP CONNECTION
        connectButton.onClick.AddListener(() =>
        {
            Debug.Log($"IP Input Field text: '{ipInputField.text}'"); // Check the actual content
            if (!string.IsNullOrEmpty(ipInputField.text))
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipInputField.text, 7777);
                if (NetworkManager.Singleton.StartClient())
                {
                    DBConnection dbConnection = FindObjectOfType<DBConnection>();
                    string username = dbConnection.GetUsername();
                    ulong localClientId = NetworkManager.Singleton.LocalClientId;
                    GameManager.Instance.SetLoggedInUsernameRpc(localClientId, username);
                   
                    Logger.Instance.LogInfo($"Attempting to connect to server at {ipInputField.text}...");
                    ResetNetworkManager();
                    DeactivateUIElements(uiElementsToDeactivateOnJoin);
                }
                else
                {
                    Logger.Instance.LogInfo("Unable to start client...");
                    ResetNetworkManager();
                }
            }
            else
            {
                Logger.Instance.LogInfo("IP address field is empty.");
               
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



    private void ResetNetworkManager()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Give some time or a frame delay to ensure complete shutdown
        StartCoroutine(RestartNetworkManager());
    }

    private IEnumerator RestartNetworkManager()
    {
        yield return new WaitForSeconds(0.1f);  // Adjust time as needed
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = gameObject.AddComponent<UnityTransport>();
    }
}
