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
using System;
using UnityEngine.Audio;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerWithRaycastControl : NetworkBehaviour
{

    public static event Action<PlayerWithRaycastControl> OnPlayerReady;

    //Footsteps
    [SerializeField] private AudioClip footstepSound;  
    private AudioSource footstepAudioSource;
    private float footstepDelay = 0.5f;  
    private float nextFootstepTime = 0f;

    [SerializeField] private AudioSource itemAudioSource; 
    [SerializeField] private AudioClip speedBoostSound; 
    [SerializeField] private AudioClip jumpBoostSound; 

    //Ball logik
    [SerializeField]
    private Transform handTransform; 
    private GameObject heldBall = null;

    //Stun Logik
    [SerializeField]
    private bool isStunned = false; 
    private float stunDuration = 5f;

    //Score variablen
    public NetworkVariable<int> networkScore = new NetworkVariable<int>(0);
    private float decimalScore;
    private bool isScoreUpdating = false;
    private Coroutine scoreCoroutine = null;

    //Stamina Variablen
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

    //Movement variablen
    [SerializeField]
    private float walkSpeed = 3.5f;

    [SerializeField]
    private float runSpeedOffset = 2.0f;

    [SerializeField]
    private float rotationSpeed = 3.5f;

    [SerializeField]
    private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    private CharacterController characterController;

    private PlayerState oldPlayerState = PlayerState.Idle;

    //Sprung variablen
    [SerializeField]
    private float jumpHeight = 2.0f;
    private bool isJumping = false;
    private float verticalVelocity = 0f;
    private int jumpCount = 0;
    private const int maxJumps = 2;

    public bool isSpeedBoostActive = false;
    public bool isJumpBoostActive = false;
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        footstepAudioSource = GetComponent<AudioSource>();
        footstepAudioSource.clip = footstepSound;
    }

    void Start()
    {
        if (IsClient && IsOwner)
        {
            staminaSlider = GameObject.FindGameObjectWithTag("StaminaSlider").GetComponent<Slider>();
            if (IsClient && IsOwner)
            {
                staminaSlider = UIManager.Instance.CreateStaminaSliderForPlayer(NetworkManager.Singleton.LocalClientId);
            }  
        }
    }

    private void PlayFootstepSound()
    {
        if (Time.time >= nextFootstepTime && characterController.isGrounded && characterController.velocity.magnitude > 0.1f)
        {
            // Bestimme das tempo vom Spielerzustand
            float speedMultiplier = (networkPlayerState.Value == PlayerState.Run) ? 1.5f : 1.0f;
            footstepAudioSource.pitch = speedMultiplier;

            // Spiele den Sound ab
            footstepAudioSource.PlayOneShot(footstepSound);

            // Berechne das nächste Abspielintervall
            nextFootstepTime = Time.time + footstepDelay / speedMultiplier;
        }
    }

    private void StartUpdatingScore()
    {
        if (scoreCoroutine == null) //nur EINE coroutine läuft damit score nicht mehrfach gezählt wird
        {
            scoreCoroutine = StartCoroutine(UpdateScoreEverySecond());
            Debug.Log("✅ Score-Update gestartet!");
        }
    }

    private void StopUpdatingScore()
    {
        if (scoreCoroutine != null) //nur stoppen wenn eine läuft
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

            if (heldBall != null && !isStunned)
            {
                UpdatePlayerScoreToServer();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            OnPlayerReady?.Invoke(this);
        }
    }

    private void UpdatePlayerScoreToServer()
    {
        if (isScoreUpdating) return;

        int userId = PlayerPrefs.GetInt("userID", 0);
        if (userId == 0)
        {
            Debug.LogError("❌ Keine Benutzer-ID gefunden! Ist der Spieler eingeloggt?");
            return;
        }
        isScoreUpdating = true; 
        StartCoroutine(SendScoreToDatabase(userId, networkScore.Value));
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
            score = 1  //score plus 1 pro sekunde
        };

        string json = JsonUtility.ToJson(scoreData);
        Debug.Log($"📤 Sende Score: {score} für User ID: {userId}");

        byte[] jsonToSend = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest("http://localhost/api/updateScore.php", "POST"))//IP MUSS MAN ANPASSEN!!!
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            isScoreUpdating = false;

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
                heldBall = other.gameObject;
                heldBall.GetComponent<Ball>().SetOwner(transform, handTransform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (heldBall != null && other.gameObject == heldBall)
        {
            Debug.Log("Spieler hat den Ballbereich verlassen");

            if (heldBall.transform.parent != handTransform)
            {
                Debug.Log("Ball wird wirklich fallen gelassen");
                DropBallServerRpc();
            }
            else
            {
                Debug.Log("Ball ist noch in der Hand.");
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
            PlayFootstepSound();
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
            networkScore.Value = Mathf.RoundToInt(decimalScore);
             StartUpdatingScore();
             Debug.Log($"NewScore: {networkScore.Value}");
         }
         else
         {
             StopUpdatingScore();
             decimalScore = networkScore.Value;
         } 
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropBallServerRpc()
    {
        if (heldBall == null)
        {
            Debug.Log("⚠️ DropBallServerRpc wurde aufgerufen, aber `heldBall` ist bereits `null`. Ignoriere den Aufruf.");
            return;
        }

        Ball ballScript = heldBall.GetComponent<Ball>();
        if (ballScript)
        {
            ballScript.ClearOwnerRpc();
        }

        Debug.Log("⚠️ Spieler hat den Ball fallen gelassen!");

        heldBall = null;
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
        move += verticalVelocity * Vector3.up * Time.deltaTime; //füge vertikale Geschwindigkeit hinzu

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
           
        }
    }

    private void ClientInput()
    {

        Vector3 inputSideStep = transform.right * Input.GetAxis("Horizontal") * walkSpeed;

        Vector3 direction = transform.forward;
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput * walkSpeed;

        bool wasRunning = networkPlayerState.Value == PlayerState.Run;
        bool isRunning = ActiveRunningActionKey() && forwardInput > 0 && !isOutOfStamina;


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

        Vector3 inputMovement = inputSideStep + inputPosition;

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

    public void ApplySpeedBoost(float duration, float multiplier)
    {
        if (!isSpeedBoostActive) // Prüfe, ob der Speed Boost nicht aktiv ist
        {
            StartCoroutine(SpeedBoostCoroutine(duration, multiplier));
            PlaySound(speedBoostSound);
        }
    }

    private IEnumerator SpeedBoostCoroutine(float duration, float multiplier)
    {
        isSpeedBoostActive = true;
        float originalSpeed = walkSpeed;
        walkSpeed *= multiplier;
        UIManager.Instance?.ShowSpeedBoostActive(true);

        yield return new WaitForSeconds(duration);

        walkSpeed = originalSpeed;
        UIManager.Instance?.ShowSpeedBoostActive(false);
        isSpeedBoostActive = false;
    }

    public void ApplyJumpBoost(float duration, float multiplier)
    {
        if (!isJumpBoostActive) // Prüfe, ob der Jump Boost nicht aktiv ist
        {
            StartCoroutine(JumpBoostCoroutine(duration, multiplier));
            PlaySound(jumpBoostSound);
        }
    }

    private IEnumerator JumpBoostCoroutine(float duration, float multiplier)
    {
        isJumpBoostActive = true;
        float originalJumpHeight = jumpHeight;
        jumpHeight *= multiplier;
        UIManager.Instance?.ShowJumpBoostActive(true);

        yield return new WaitForSeconds(duration);

        jumpHeight = originalJumpHeight;
        UIManager.Instance?.ShowJumpBoostActive(false);
        isJumpBoostActive = false;
    }
    private void PlaySound(AudioClip clip)
    {
        if (itemAudioSource != null && clip != null)
        {
            itemAudioSource.PlayOneShot(clip);
        }
    }
}