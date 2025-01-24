using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro;

public class Bomb : NetworkBehaviour
{
    public TMP_Text bombTimerText;

    [SerializeField]
    private float transferRadius = 2.0f;

    [SerializeField]
    private float transferCooldown = 1.0f;

    [SerializeField]
    private float explosionTimer = 10.0f;

    private PlayerBombHandler currentOwner;
    private bool canTransfer = true;
    private float currentTimer;

    private void Start()
    {
        if (IsServer)
        {
            currentTimer = explosionTimer; //Timer für Bombe
        }

        
        var textObj = GameObject.FindWithTag("BombTimer");
        if (textObj != null)
        {
            bombTimerText = textObj.GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (!IsServer || currentOwner == null || !currentOwner.IsAlive) return;

        currentTimer -= Time.deltaTime;
        UpdateBombTimerClientRpc(currentTimer);

        if (currentTimer <= 0)
        {
            Explode();
            return;
        }

        CheckForNearbyPlayers();
    }

    [ClientRpc]
    private void UpdateBombTimerClientRpc(float time)
    {
        if (bombTimerText != null)
        {
            bombTimerText.text = "Bomb Timer: " + Mathf.CeilToInt(time).ToString();
        }
    }

    public void AssignOwner(PlayerBombHandler newOwner)
    {
        if (newOwner == null || !newOwner.IsAlive) return;

        currentOwner = newOwner;

        // Position für den neuen "Owner" für die Bombe
        transform.SetParent(newOwner.transform);

        //Bomben Spawn Position auf dem Player
        transform.localPosition = new Vector3(0.2f, 1.2f, 0.2f);

       // Debug.Log($"Bomb assigned to Player {newOwner.GetPlayerID()}");
    }

    private void CheckForNearbyPlayers()
    {
        if (!canTransfer) return;

        Collider[] hitColliders = Physics.OverlapSphere(currentOwner.transform.position, transferRadius);

        foreach (var hitCollider in hitColliders)
        {
            PlayerBombHandler potentialNewOwner = hitCollider.GetComponent<PlayerBombHandler>();

            if (potentialNewOwner != null && potentialNewOwner != currentOwner && potentialNewOwner.IsAlive)
            {
                TransferBomb(potentialNewOwner);
                break;
            }
        }
    }

    private void TransferBomb(PlayerBombHandler newOwner)
    {
        AssignOwner(newOwner);
        StartCoroutine(TransferCooldown());
    }

    private IEnumerator TransferCooldown()
    {
        canTransfer = false;
        yield return new WaitForSeconds(transferCooldown);
        canTransfer = true;
    }

    private void Explode()
    {
        GameManager.Instance.PlayerDied(currentOwner);
        DestroyBombServerRpc();
    }

    [ServerRpc]
    private void DestroyBombServerRpc()
    {
        NetworkObject.Despawn();
        Destroy(gameObject);
    }
}
