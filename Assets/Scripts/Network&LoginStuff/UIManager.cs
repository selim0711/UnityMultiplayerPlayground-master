using DilmerGames.Core.Singletons;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using Unity.Netcode.Transports.UTP;
using System.Data.Common;
using System.Collections;
using UnityEngine.Audio;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private AudioMixer mainAudioMixer;

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


    private Slider staminaSliderPrefab;
    [SerializeField] private Transform uiParent;

    [SerializeField] private GameObject speedBoostIndicator;
    [SerializeField] private GameObject jumpBoostIndicator;

    [SerializeField]
    private TMP_InputField ipInputField;  // Referenz zum IP-Adresse Eingabefeld
    [SerializeField]
    private Button connectButton;  // Verbindungsbutton


    private bool hasServerStarted;

    [SerializeField]
    private GameObject[] uiElementsToDeactivateOnHost;
    [SerializeField]
    private GameObject[] uiElementsToDeactivateOnJoin;

    //Deaktivieren der Musik
    public void SetMusicVolume(float volume)
    {
        mainAudioMixer.SetFloat("MusicVolume", volume);
    }
    public void SetGameMusicVolume(float volume)
    {
        mainAudioMixer.SetFloat("GameMusicVolume", volume);
    }

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
        mainAudioMixer.SetFloat("MusicVolume", 0);
        mainAudioMixer.SetFloat("GameMusicVolume", -80);
        Cursor.visible = true;
    }
    
    /*
void Update()
{
        playersInGameText.text = $"Players in game: {GameManager.Instance.GetAlivePlayersCount()}";
    } */
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

    private string GetLocalIPAddress()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    void Start()
    {
        // START SERVER
        startServerButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("✅ Server gestartet...");
                SetupHostOrServerCallbacks();
                SetMusicVolume(-80);
                SetGameMusicVolume(0);
            }
            else
            {
                Debug.LogError("❌ Server konnte nicht gestartet werden!");
            }
        });

        // START HOST
        startHostButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log($"✅ Host gestartet... IP: {GetLocalIPAddress()}");
                DeactivateUIElements(uiElementsToDeactivateOnHost);
                SetMusicVolume(-80);
                SetGameMusicVolume(0);

                StartCoroutine(WaitForGameManagerAndSetUsername());
            }
            else
            {
                Debug.LogError("❌ Host konnte nicht gestartet werden!");
            }
        });

        // START CLIENT (Relay)
        startClientButton?.onClick.AddListener(async () =>
        {
            if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCodeInput.text))
            {
                await RelayManager.Instance.JoinRelay(joinCodeInput.text);
            }

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("✅ Client gestartet...");
                SetupClientCallbacks();
                DeactivateUIElements(uiElementsToDeactivateOnJoin);
                SetMusicVolume(-80);
                SetGameMusicVolume(0);
            }
            else
            {
                Debug.LogError("❌ Client konnte nicht gestartet werden!");
            }
        });

        // CONNECT VIA IP
        connectButton?.onClick.AddListener(() =>
        {
            StartCoroutine(ConnectToServerByIP());
        });

        // CLIENT CONNECTION CALLBACK
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            Debug.Log($"🟢 Client {id} verbunden.");
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
            Debug.Log($"🟢 [Server] Client {clientId} verbunden.");

            if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"[Host] Host ClientId: {clientId}");
            }
        };
    }

    private void SetupClientCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log($"🟢 [Client] Erfolgreich mit dem Server verbunden: {clientId}");

            StartCoroutine(WaitForGameManagerAndSetUsername());
        };
    }
    private IEnumerator WaitForGameManagerAndSetUsername()
    {
        float waitTime = 3f;
        while (GameManager.Instance == null && waitTime > 0)
        {
            Debug.Log("⏳ Warte auf GameManager...");
            yield return new WaitForSeconds(0.5f);
            waitTime -= 0.5f;
        }

        if (GameManager.Instance != null)
        {
            Debug.Log("✅ GameManager gefunden!");

            DBConnection dbConnection = FindObjectOfType<DBConnection>();
            if (dbConnection != null)
            {
                string username = dbConnection.GetUsername();
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                GameManager.Instance.SetLoggedInUsernameRpc(localClientId, username);
                Debug.Log($"📡 Username '{username}' für Client {localClientId} gesetzt.");
            }
            else
            {
                Debug.LogError("❌ DBConnection nicht gefunden!");
            }
        }
        else
        {
            Debug.LogError("❌ GameManager nach Wartezeit nicht gefunden!");
        }
    }
    private IEnumerator ConnectToServerByIP()
    {
        string ipAddress = ipInputField.text.Trim();
        int port = 7777;

        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("❌ IP-Adresse ist leer!");
            yield break;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("❌ UnityTransport nicht gefunden!");
            yield break;
        }

        Debug.Log($"🔄 Verbinde zu {ipAddress}:{port}...");

        transport.SetConnectionData(ipAddress, (ushort)port);

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("✅ Client gestartet! Warte auf Verbindung...");

            float timeout = 5f;
            while (!NetworkManager.Singleton.IsConnectedClient && timeout > 0)
            {
                yield return new WaitForSeconds(0.5f);
                timeout -= 0.5f;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("✅ Erfolgreich mit Server verbunden!");
                DeactivateUIElements(uiElementsToDeactivateOnJoin);
                SetMusicVolume(-80);
                SetGameMusicVolume(0);

                StartCoroutine(WaitForGameManagerAndSetUsername());
            }
            else
            {
                Debug.LogError("❌ Verbindung zum Server fehlgeschlagen!");
                NetworkManager.Singleton.Shutdown();
            }
        }
        else
        {
            Debug.LogError("❌ Client konnte nicht gestartet werden!");
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

    public void ShowSpeedBoostActive(bool isActive)
    {
        if (speedBoostIndicator != null)
        {
            speedBoostIndicator.SetActive(isActive);
        }
    }
    public void ShowJumpBoostActive(bool isActive)
    {
        if (jumpBoostIndicator != null)
        {
            jumpBoostIndicator.SetActive(isActive);
        }
    }
}
