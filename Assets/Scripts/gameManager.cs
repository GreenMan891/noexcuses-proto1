using System.Collections;
using System.Collections.Generic;
using FishNet.Example.Scened;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using FishNet.Component.Transforming;
using FishNet.Object.Synchronizing;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SocialPlatforms.Impl;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject ClientServerManager;

    private List<GameObject> gamers = new List<GameObject>();

    [SerializeField] private Canvas canvas;

    [SerializeField] private Image player1ScoreImage;
    [SerializeField] private Image player2ScoreImage;

    [SerializeField] private Image winImage;
    [SerializeField] private Image loseImage;

    [SerializeField] private Sprite none;
    [SerializeField] private Sprite one;
    [SerializeField] private Sprite two;
    [SerializeField] private Sprite three;

    [SerializeField] private Transform[] spawnPoints;

    public int player1Score = 0;
    public int player2Score = 0;
    private int currentRound = 0;



    //list of things to do for the prototype:
    // proper networking
    // winning screen if you hit 3 points
    // randomize obstacles
    // basic title screen
    // normalize ui
    // proper rotation

    //things for the full game:
    // timer
    // better movement
    // matchmaking 
    // art style (cream white?)
    // sound effects 
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
            AddWinImage(winner.Owner.ClientId);
        }
        StartCoroutine(DelayedReset());
    }

    private IEnumerator DelayedReset()
    {
        yield return new WaitForSeconds(4f);
        ResetRound();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseScore(playerState winner)
    {
        if (winner.Owner.ClientId == 0)
        {
            player1Score++;
        }
        else
        {
            player2Score++;
        }
        ChangePlayerScores(player1Score, player2Score);
        Debug.Log($"Player 1: {player1Score} Player 2: {player2Score}");
    }

    [ObserversRpc]
    private void ChangePlayerScores(int score1, int score2)
    {
        if (score1 == 1)
        {
            player1ScoreImage.sprite = one;
        }
        else if (score1 == 2)
        {
            player1ScoreImage.sprite = two;
        }
        else if (score1 == 3)
        {
            player1ScoreImage.sprite = three;
        }
        if (score2 == 1)
        {
            player2ScoreImage.sprite = one;
        }
        else if (score2 == 2)
        {
            player2ScoreImage.sprite = two;
        }
        else if (score2 == 3)
        {
            player2ScoreImage.sprite = three;
        }
    }

    [ObserversRpc]
    private void AddWinImage(int clientId)
    {
        if (ClientManager.Connection.ClientId == clientId) // Compare with LocalClientId
        {
            winImage.gameObject.SetActive(true);
            loseImage.gameObject.SetActive(false);
        }
        else
        {
            loseImage.gameObject.SetActive(true);
            winImage.gameObject.SetActive(false);
        }

    }


    [ServerRpc(RequireOwnership = false)]
    private void ResetRound()
    {
        currentRound++;
        removeWinImagery();
        Debug.Log($"Round {currentRound} starting...");
        playerState[] players = FindObjectsOfType<playerState>();
        RandomizeObjects();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].ResetPlayer(spawnPoints[i].position);
        }
    }

    [ObserversRpc]
    private void removeWinImagery()
    {
        winImage.gameObject.SetActive(false);
        loseImage.gameObject.SetActive(false);
    }

    private void RandomizeObjects()
    {
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
