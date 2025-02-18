using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SocialPlatforms.Impl;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerWithRaycastControl : NetworkBehaviour
{
    [SerializeField]
    private Transform handTransform; // Transform, where the ball should be positioned when held


    private GameObject heldBall = null;

    private float decimalScore;
    [SerializeField]

    private int score = 0; // Player's score
    

    [SerializeField]
    private bool isStunned = false; // Stun-Zustand des Spielers
    private float stunDuration = 5f;



    private bool isUpdatingScore = false;

    private bool isScoreUpdating = false;

    private Coroutine scoreCoroutine = null;







    [SerializeField]
    private NetworkVariable<float> networkPlayerStamina = new NetworkVariable<float>(100f);

    [SerializeField]
    private float maxStamina = 100f;

    [SerializeField]
    private float staminaRegenerationRate = 5f;

    [SerializeField]
    private float staminaDepletionRate = 20f;

    private Slider staminaSlider;

    private bool isOutOfStamina = false;

    [SerializeField]
    private float walkSpeed = 3.5f;

    [SerializeField]
    private float runSpeedOffset = 2.0f;

    [SerializeField]
    private float rotationSpeed = 3.5f;

    [SerializeField]
    private Vector2 defaultInitialPositionOnPlane = new Vector2(-7, -7);


    [SerializeField]
    private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();


    [SerializeField]
    private NetworkVariable<float> networkPlayerHealth = new NetworkVariable<float>(1000);

    [SerializeField]
    private NetworkVariable<float> networkPlayerPunchBlend = new NetworkVariable<float>();

    [SerializeField]
    private GameObject leftHand;

    [SerializeField]
    private GameObject rightHand;

    [SerializeField]
    private float minPunchDistance = 1.0f;

    private CharacterController characterController;

    private Vector3 oldInputPosition = Vector3.zero;
    private Vector3 oldInputRotation = Vector3.zero;
    private PlayerState oldPlayerState = PlayerState.Idle;

    private Animator animator;

    [SerializeField]
    private float jumpHeight = 2.0f;
    private bool isJumping = false;
    private float verticalVelocity = 0f;
    private int jumpCount = 0;
    private const int maxJumps = 2;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (IsClient && IsOwner)
        {
     //       transform.position = new Vector3(Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y), 0,
             //      Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y));


            staminaSlider = GameObject.FindGameObjectWithTag("StaminaSlider").GetComponent<Slider>();
            if (IsClient && IsOwner)
            {
                staminaSlider = UIManager.Instance.CreateStaminaSliderForPlayer(NetworkManager.Singleton.LocalClientId);
            }
           

        }
    }
    private void StartUpdatingScore()
    {
        if (scoreCoroutine == null) // ✅ Stelle sicher, dass nur EINE Coroutine läuft!
        {
            scoreCoroutine = StartCoroutine(UpdateScoreEverySecond());
            Debug.Log("✅ Score-Update gestartet!");
        }
    }

    private void StopUpdatingScore()
    {
        if (scoreCoroutine != null) // ✅ Nur stoppen, wenn eine läuft
        {
            StopCoroutine(scoreCoroutine);
            scoreCoroutine = null;
            Debug.Log("❌ Score-Update gestoppt!");
        }
    }

    private IEnumerator UpdateScoreEverySecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (heldBall != null && !isStunned) // 🏀 Nur wenn der Ball gehalten wird!
            {
                UpdatePlayerScoreToServer();
            }
        }
    }

    private void UpdatePlayerScoreToServer()
    {
        if (isScoreUpdating) return; // 🛑 Falls bereits eine Anfrage läuft, breche ab!

        int userId = PlayerPrefs.GetInt("userID", 0);
        if (userId == 0)
        {
            Debug.LogError("❌ Keine Benutzer-ID gefunden! Ist der Spieler eingeloggt?");
            return;
        }

        isScoreUpdating = true; // ✅ Sperre aktivieren
     // StartCoroutine(SendScoreToDatabase(userId, score));
        StartCoroutine(SendScoreToDatabase(userId, score));
    }


    [System.Serializable]
    public class ScoreData
    {
        public int id;
        public int score;
    }


    public IEnumerator SendScoreToDatabase(int userId, int score)
    {
        ScoreData scoreData = new ScoreData
        {
            id = userId,
            score = 1  // 🟢 Score immer nur +1 pro Sekunde senden
        };

        string json = JsonUtility.ToJson(scoreData);
        Debug.Log($"📤 Sende Score: {score} für User ID: {userId}");

        byte[] jsonToSend = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest("http://192.168.8.157/api/updateScore.php", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            isScoreUpdating = false; // ✅ Sperre wieder deaktivieren!

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Fehler beim Senden des Scores: " + www.error);
            }
            else
            {
                Debug.Log("✅ Score erfolgreich aktualisiert: " + www.downloadHandler.text);
            }
        }
    }




    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (heldBall != null && !isStunned)
            {
                DropBallServerRpc();
                StartCoroutine(StunPlayer(stunDuration));
            }
        }
        else if (other.gameObject.CompareTag("Ball") && heldBall == null && !isStunned)
        {
            Ball ballScript = other.GetComponent<Ball>();
            if (ballScript && ballScript.CanInteract(NetworkManager.Singleton.LocalClientId))
            {
                ballScript.InteractWithBallRpc(NetworkManager.Singleton.LocalClientId);
                heldBall = other.gameObject; // Halte den Ball
                heldBall.GetComponent<Ball>().SetOwner(transform, handTransform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (heldBall != null && other.gameObject == heldBall)
        {
            Debug.Log("⚠ Spieler hat den Ballbereich verlassen.");

            // ✅ Nur DropBallServerRpc aufrufen, wenn der Ball NICHT mehr in der Hand ist!
            if (heldBall.transform.parent != handTransform)
            {
                Debug.Log("⚠️ Ball wird wirklich fallen gelassen.");
                DropBallServerRpc();
            }
            else
            {
                Debug.Log("✅ Ball ist noch in der Hand, nichts tun.");
            }
        }
    } 


    private IEnumerator StunPlayer(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }












    private void Update()
    {
        if (IsOwner)
        {
            ClientInput();
            UpdateStaminaUI();
            
            //HandleMouseRotation();
        }

        if (IsServer)
        {
            HandleStaminaRegenerationAndDepletion();
        }


        HandleGravity();
        ClientMoveAndRotate();
        ClientVisuals();
        HandleJump();

         if (heldBall != null && !isStunned)
         {
             decimalScore += 1 * Time.deltaTime;
             score = Mathf.RoundToInt(decimalScore);
             StartUpdatingScore(); // ✅ Score-Update nur starten, wenn Ball gehalten wird
             Debug.Log($"NewScore: {score}");
         }
         else
         {
             StopUpdatingScore(); // ✅ Stoppe Score-Update, wenn der Ball losgelassen wird
             decimalScore = score;
         } 
    }


    [ServerRpc(RequireOwnership = false)]
    public void DropBallServerRpc()
    {
        if (heldBall == null)
        {
            Debug.Log("⚠️ DropBallServerRpc wurde aufgerufen, aber `heldBall` ist bereits `null`. Ignoriere den Aufruf.");
            return; // 🛑 Falls der Ball bereits entfernt wurde, nichts tun!
        }

        Ball ballScript = heldBall.GetComponent<Ball>();
        if (ballScript)
        {
            ballScript.ClearOwnerRpc();
        }

        Debug.Log("⚠️ Spieler hat den Ball fallen gelassen!");

        heldBall = null; // ✅ Sicherstellen, dass `heldBall` wirklich entfernt wurde
    }

















    private void HandleGravity()
    {
        if (!characterController.isGrounded)
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        else if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = 0;
        }
    }

    private void ClientMoveAndRotate()
    {
        Vector3 move = networkPositionDirection.Value * Time.deltaTime;
        move += verticalVelocity * Vector3.up * Time.deltaTime; // Füge vertikale Geschwindigkeit hinzu

        characterController.Move(move);

        if (networkRotationDirection.Value != Vector3.zero)
        {
            transform.Rotate(networkRotationDirection.Value, Space.World);
        }
    }


    private void ClientVisuals()
    {
        if (oldPlayerState != networkPlayerState.Value)
        {
            oldPlayerState = networkPlayerState.Value;
            animator.SetTrigger($"{networkPlayerState.Value}");
        }
    }

    private void ClientInput()
    {
        // Seitliche Bewegungen
        Vector3 inputSideStep = transform.right * Input.GetAxis("Horizontal") * walkSpeed;

        // Vorwärts/Rückwärts-Bewegung
        Vector3 direction = transform.forward;
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput * walkSpeed;

        // Zustandserkennung für Laufen
        bool wasRunning = networkPlayerState.Value == PlayerState.Run;
        bool isRunning = ActiveRunningActionKey() && forwardInput > 0 && !isOutOfStamina;

        // Anwendung des Laufgeschwindigkeitsbonus, wenn der Spieler rennt und nicht außer Atem ist
        if (isRunning)
        {
            inputPosition *= runSpeedOffset;
        }

        if (isRunning)
        {
            if (networkPlayerStamina.Value > 0)
            {
                RequestStaminaDepletionServerRpc(Time.deltaTime);
            }
            else
            {
                isOutOfStamina = true;
                UpdatePlayerStateServerRpc(PlayerState.Walk);
            }
        }
        else if (!isRunning && wasRunning)
        {
            RequestStaminaRegenerationServerRpc(Time.deltaTime);
        }

        if (isOutOfStamina && networkPlayerStamina.Value >= 30)
        {
            isOutOfStamina = false;
        }

        // Kombinieren der seitlichen und vorwärts/rückwärts Bewegungen
        Vector3 inputMovement = inputSideStep + inputPosition;

        // Aktualisieren des Zustands basierend auf den Eingaben und Ausdauer
        UpdatePlayerMovementState(forwardInput, Input.GetAxis("Horizontal"), isRunning);

        if (inputMovement != Vector3.zero || isJumping)
        {
            UpdateClientPositionAndRotationServerRpc(inputMovement, Vector3.zero, verticalVelocity);
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (characterController.isGrounded || jumpCount < maxJumps)
            {
                verticalVelocity = CalculateJumpVerticalSpeed();
                isJumping = true;
                jumpCount++;  
                UpdateClientPositionAndRotationServerRpc(networkPositionDirection.Value, Vector3.zero, verticalVelocity);
            }
        }

    }
    private float CalculateJumpVerticalSpeed()
    {
        return Mathf.Sqrt(2 * jumpHeight * Physics.gravity.magnitude);
    }

    private void HandleJump()
    {
        if (characterController.isGrounded)
        {
            if (isJumping)
            {
                isJumping = false;
            }
            jumpCount = 0; 
        }
    }

    private void UpdatePlayerMovementState(float forwardInput, float sideInput, bool isRunning)
    {
        if (forwardInput == 0 && sideInput == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }
        else if (isRunning)
        {
            UpdatePlayerStateServerRpc(PlayerState.Run);
        }
        else if (forwardInput != 0 || sideInput != 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        }
        else if (forwardInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);
        }
    }








    private static bool ActiveRunningActionKey()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    [Rpc(SendTo.Server)]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPosition, Vector3 newRotation, float newVerticalVelocity)
    {
        networkPositionDirection.Value = newPosition;
        networkRotationDirection.Value = newRotation;
        verticalVelocity = newVerticalVelocity; // Direktes Update der vertikalen Geschwindigkeit auf dem Server
    }

    [Rpc(SendTo.Server)]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        networkPlayerState.Value = state;
        if (state == PlayerState.Punch)
        {
            networkPlayerPunchBlend.Value = Random.Range(0.0f, 1.0f);
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = networkPlayerStamina.Value / maxStamina;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestStaminaDepletionServerRpc(float deltaTime)
    {
        if (networkPlayerState.Value == PlayerState.Run && networkPlayerStamina.Value > 0)
        {
            networkPlayerStamina.Value = Mathf.Max(0, networkPlayerStamina.Value - staminaDepletionRate * deltaTime);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestStaminaRegenerationServerRpc(float deltaTime)
    {
        if (networkPlayerState.Value != PlayerState.Run)
        {
            networkPlayerStamina.Value = Mathf.Min(maxStamina, networkPlayerStamina.Value + staminaRegenerationRate * deltaTime);
        }
    }

    private void HandleStaminaRegenerationAndDepletion()
    {
        if (networkPlayerState.Value == PlayerState.Run && networkPlayerStamina.Value > 0)
        {
            networkPlayerStamina.Value = Mathf.Max(0, networkPlayerStamina.Value - staminaDepletionRate * Time.deltaTime);
            if (networkPlayerStamina.Value == 0)
            {
                isOutOfStamina = true;
                UpdatePlayerStateServerRpc(PlayerState.Walk);
            }
        }
        else
        {
            if (networkPlayerState.Value != PlayerState.Run)
            {
                networkPlayerStamina.Value = Mathf.Min(maxStamina, networkPlayerStamina.Value + staminaRegenerationRate * Time.deltaTime);
                if (isOutOfStamina && networkPlayerStamina.Value >= 30)
                {
                    isOutOfStamina = false;
                }
            }
        }
    }
}






