using Unity.Netcode;
using UnityEngine;

public class PlayerSetup : NetworkBehaviour
{
    /*
    [SerializeField]
    private string localPlayerLayer = "LocalPlayer";
    [SerializeField]
    private string remotePlayerLayer = "RemotePlayer";

    void Start()
    {
        if (IsOwner)
        {
            // Setzen Sie den Layer für den lokalen Spieler und alle Kindobjekte
            SetLayerRecursively(gameObject, LayerMask.NameToLayer(localPlayerLayer));
        }
        else
        {
            // Setzen Sie den Layer für entfernte Spieler
            SetLayerRecursively(gameObject, LayerMask.NameToLayer(remotePlayerLayer));
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    } */
}
