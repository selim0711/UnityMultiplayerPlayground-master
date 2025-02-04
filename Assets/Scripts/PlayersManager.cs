using DilmerGames.Core.Singletons;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using TMPro;

public class PlayersManager : NetworkSingleton<PlayersManager>
{

    // NetworkVariable<int> playersInGame = new NetworkVariable<int>();
    NetworkVariable<int> playersInGame = new NetworkVariable<int>(0);
    public int PlayersInGame
    {
        get
        {
            return playersInGame.Value;
        }
    }

   void Start()
{
    if (!NetworkManager.Singleton.IsServer)
    {
      //  Debug.LogError("PlayersManager is running on a non-server instance.");
        return;
    }

    NetworkManager.Singleton.OnClientConnectedCallback += (id) => {
        playersInGame.Value++;
     //   Debug.Log($"Player connected: {id}. Total players: {playersInGame.Value}");
    };

    NetworkManager.Singleton.OnClientDisconnectCallback += (id) => {
        playersInGame.Value--;
     //   Debug.Log($"Player disconnected: {id}. Total players: {playersInGame.Value}");
    };
}
}
