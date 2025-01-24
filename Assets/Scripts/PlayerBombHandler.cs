using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro; 

public class PlayerBombHandler : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerTag;

    [SerializeField]
    private GameObject loseUI; 
    [SerializeField]
    private GameObject winUI; 

    [SerializeField]
    private GameObject winnerAnnouncementUI;

    [SerializeField]
    private TMP_Text winnerAnnouncementText; 

    NetworkPlayer player = null;

    [SerializeField]
    private float bombTransferRadius = 2.0f;

    [SerializeField]
    private float bombCooldown = 1.0f;

    private bool hasBomb = false;
    private bool canTransferBomb = true;
    private bool isAlive = true;


    public bool IsAlive => isAlive;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        player = GetComponent<NetworkPlayer>();
    }

    [ClientRpc]
    public void ShowWinUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        winUI?.SetActive(true);
        loseUI?.SetActive(false);
    }

    [ClientRpc]
    public void ShowLoseUIClientRpc(ClientRpcParams clientRpcParams = default)
    {
        loseUI?.SetActive(true);
        winUI?.SetActive(false);
    }

    [ClientRpc]
    public void ShowWinnerAnnouncementClientRpc(string winnerName, ClientRpcParams clientRpcParams = default)
    {
        if (winnerAnnouncementUI != null)
        {
            TMP_Text announcementText = winnerAnnouncementUI.GetComponentInChildren<TMP_Text>();
            if (announcementText != null)
            {
                announcementText.text = $"{winnerName} won the game!";
            }
            winnerAnnouncementUI.SetActive(true); // Make sure this line is being reached
        }
    }

    public void OnPlayerDied()
    {
        isAlive = false;
        GetComponent<Collider>().enabled = false;
        UpdateRendererStateClientRpc(false);
        UpdateTagVisibilityClientRpc(false);
        Debug.Log("Trying to remove player with ID: " + NetworkObjectId);
        Debug.Log("Players before removal: " + NetworkPlayer.playerList.Count);

        if (NetworkPlayer.playerList.ContainsKey(NetworkObjectId))
        {
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Debug.Log("Player removed successfully.");
        }
        else
        {
            Debug.Log("Player not found in list.");
        }

        Debug.Log("Players after removal: " + NetworkPlayer.playerList.Count);
    }

    [ClientRpc]
    private void UpdateRendererStateClientRpc(bool isVisible)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = isVisible;
        }
    }

    [ClientRpc]
    private void UpdateTagVisibilityClientRpc(bool isVisible)
    {
        if (playerTag != null)
        {
            playerTag.SetActive(isVisible);
        }
    }

    private void Update()
    {
        if (!IsServer || !isAlive || !hasBomb) return;

        CheckForNearbyPlayers();
    }

    private void CheckForNearbyPlayers()
    {
        if (!canTransferBomb) return;

        Collider[] nearbyPlayers = Physics.OverlapSphere(transform.position, bombTransferRadius);
        foreach (var collider in nearbyPlayers)
        {
            var otherPlayer = collider.GetComponent<PlayerBombHandler>();
            if (otherPlayer != null && otherPlayer != this && otherPlayer.IsAlive && !otherPlayer.hasBomb)
            {
                PlayerData pd = GameManager.Instance.GetPlayerData(otherPlayer.OwnerClientId);
                //Debug.Log($"Found player {pd.playerName} with ID {pd.playerID}");
                TransferBombServerRpc(otherPlayer.OwnerClientId);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransferBombServerRpc(ulong targetPlayerId)
    {
        var targetPlayer = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetPlayerId]
            .GetComponent<PlayerBombHandler>();

        if (targetPlayer != null && targetPlayer.IsAlive && !targetPlayer.hasBomb)
        {
            targetPlayer.ReceiveBomb();
            RemoveBomb();
            StartCoroutine(BombCooldown());
        }
    }

    private IEnumerator BombCooldown()
    {
        canTransferBomb = false;
        yield return new WaitForSeconds(bombCooldown);
        canTransferBomb = true;
    }

    public void ReceiveBomb()
    {
        hasBomb = true;
    }

    private void RemoveBomb()
    {
        hasBomb = false;
    }
    

    public ulong GetPlayerID()
    {
        return player.playerID;
    }

}
