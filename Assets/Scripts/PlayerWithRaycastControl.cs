using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cinemachine;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerWithRaycastControl : NetworkBehaviour
{
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

    // Zustand für den Spieler, ob er sich in der Luft befindet
    private bool isJumping = false;
    private float verticalVelocity = 0f;
    private int jumpCount = 0;
    private const int maxJumps = 2;
    /*
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;  // Neue Sensibilitätseinstellung
    */
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
            // Hier fügst du die aktuelle vertikale Geschwindigkeit hinzu, die auch über das Netzwerk synchronisiert werden muss.
            UpdateClientPositionAndRotationServerRpc(inputMovement, Vector3.zero, verticalVelocity);
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (characterController.isGrounded || jumpCount < maxJumps)
            {
                verticalVelocity = CalculateJumpVerticalSpeed();
                isJumping = true;
                jumpCount++;  // Zähle jeden Sprung
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
            jumpCount = 0;  // Sprungzähler zurücksetzen, wenn der Spieler den Boden berührt
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






