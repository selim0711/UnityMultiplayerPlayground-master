using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    private Vector2 defaultInitialPositionOnPlane = new Vector2(-4, 4);

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

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (IsClient && IsOwner)
        {
            transform.position = new Vector3(Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y), 0,
                   Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y));

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
        }

        if (IsServer)
        {
            HandleStaminaRegenerationAndDepletion();
        }

        ClientMoveAndRotate();
        ClientVisuals();
    }

    private void ClientMoveAndRotate()
    {
        if (networkPositionDirection.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPositionDirection.Value);
        }
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
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput;

        bool wasRunning = networkPlayerState.Value == PlayerState.Run;

        if (isOutOfStamina && networkPlayerStamina.Value >= 30)
        {
            isOutOfStamina = false;  
        }

        if (forwardInput == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }
        else if (!ActiveRunningActionKey() && forwardInput > 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        }
        else if (ActiveRunningActionKey() && forwardInput > 0)
        {
            if (!isOutOfStamina)
            {
                inputPosition = direction * runSpeedOffset;
                UpdatePlayerStateServerRpc(PlayerState.Run);
            }
            else
            {
                
                UpdatePlayerStateServerRpc(PlayerState.Walk);
            }
        }
        else if (forwardInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);
        }

        if (oldInputPosition != inputPosition || oldInputRotation != inputRotation)
        {
            oldInputPosition = inputPosition;
            oldInputRotation = inputRotation;
            UpdateClientPositionAndRotationServerRpc(inputPosition * walkSpeed, inputRotation * rotationSpeed);
        }

        if (networkPlayerState.Value == PlayerState.Run)
        {
            if (networkPlayerStamina.Value > 0)
            {
                RequestStaminaDepletionServerRpc(Time.deltaTime);  
            }
            else if (!wasRunning || networkPlayerStamina.Value == 0)
            {
                isOutOfStamina = true; 
                UpdatePlayerStateServerRpc(PlayerState.Walk);
            }
        }

        if (networkPlayerState.Value != PlayerState.Run && wasRunning)
        {
            RequestStaminaRegenerationServerRpc(Time.deltaTime);
        }
    }

    private static bool ActiveRunningActionKey()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    [Rpc(SendTo.Server)]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPosition, Vector3 newRotation)
    {
        networkPositionDirection.Value = newPosition;
        networkRotationDirection.Value = newRotation;
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
