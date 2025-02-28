using System.Collections;
using System.Collections.Generic;
using FishNet.Example.Scened;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using FishNet.Component.Transforming;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject ClientServerManager;

    private List<GameObject> players = new List<GameObject>();
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPlayer(GameObject player)
    {
        if (!IsServer) return;
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"Player registered: {player.name}");

            // Check if both players have joined
            if (players.Count == 2)
            {
                StartGame();
            }
        }
    }

    [ServerRpc]
    private void StartGame()
    {

        if (!IsServer) return;

        Debug.Log("Both players are present. Starting game...");

        // Example: Teleport players to designated starting spawn points if available.
        if (spawnPoints != null && spawnPoints.Length >= 2)
        {   
            ClientServerManager.GetComponent<PlayerController>().TeleportToPositionServerRpc(players[0], new Vector3(50,50,50));
            ClientServerManager.GetComponent<PlayerController>().TeleportToPositionServerRpc(players[1], new Vector3(80,80,80));
        }

        // Additional game-start logic (e.g., starting timers, enabling controls, etc.)
    }


    // [ServerRpc(RequireOwnership = false)]
    // public void TeleportPlayer(NetworkObject playerObj, int spawnIndex)
    // {
    //     if (spawnPoints == null || spawnPoints.Length == 0) return;
    //     if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length) return;

    //     if (players.Contains(playerObj.gameObject))
    //     {
    //         playerObj.GetComponent<NetworkTransform>().transform.position = spawnPoints[spawnIndex].position;
    //         //transform.position = spawnPoints[spawnIndex].position;
    //         Debug.Log($"Teleported {playerObj.gameObject.name} to spawn {spawnPoints[spawnIndex]}");
    //         Debug.Log("current position of this player: " + playerObj.transform.position);
    //     }
    // }
}
