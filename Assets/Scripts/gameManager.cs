using System.Collections;
using System.Collections.Generic;
using FishNet.Example.Scened;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using FishNet.Component.Transforming;
using FishNet.Object.Synchronizing;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject ClientServerManager;

    private List<GameObject> gamers = new List<GameObject>();
    [SerializeField] private Transform[] spawnPoints;

    public int player1Score = 0;
    public int player2Score = 0;
    private int currentRound = 0;


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

    [ServerRpc(RequireOwnership = false)]
    public void HandleRoundReset(playerState deadPlayer)
    {
        if (!IsServer) return;
        Debug.Log("about to run round reset");
        playerState[] players = FindObjectsOfType<playerState>();
        playerState winner = null;
        foreach (playerState player in players)
        {
            gamers.Add(player.gameObject);
            if (player != deadPlayer)
            {
                winner = player;
            }
        }

        if (winner != null)
        {
            IncreaseScore(winner);
            Debug.Log($"{winner.gameObject.name} wins the round!");
            AnnounceWinner(winner.Owner.ClientId);
        }

        ResetRound();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseScore(playerState winner) {
       if ( winner.Owner.ClientId == 0 ) {
           player1Score++;
       } else {
           player2Score++;
       }
       Debug.Log($"Player 1: {player1Score} Player 2: {player2Score}");
    }

    [ObserversRpc]
    private void AnnounceWinner(int clientId) {

        Debug.Log($"Announcing winner to client {clientId}");
    }


    [ServerRpc(RequireOwnership = false)]
    private void ResetRound() {
        currentRound++;
        Debug.Log($"Round {currentRound} starting...");
        playerState[] players = FindObjectsOfType<playerState>();
        RandomizeObjects();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].ResetPlayer(spawnPoints[i].position);
        }
    }

    private void RandomizeObjects() {
        //idk yet we will see
    }
    // Example: Teleport players to designated starting spawn points if available.
    // if (spawnPoints != null && spawnPoints.Value.Length >= 2)
    // {   
    // }

    // Additional game-start logic (e.g., starting timers, enabling controls, etc.)


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
