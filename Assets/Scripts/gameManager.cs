using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using FishNet.Component.Transforming;
using FishNet.Object.Synchronizing;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using FishNet.Managing.Scened;
using FishNet.Managing;


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
    [SerializeField] private Image finalWinImage;
    [SerializeField] private Image finalLoseImage;

    [SerializeField] private Sprite none;
    [SerializeField] private Sprite one;
    [SerializeField] private Sprite two;
    [SerializeField] private Sprite three;

    [SerializeField] private Image countDown3;
    [SerializeField] private Image countDown2;
    [SerializeField] private Image countDown1;
    [SerializeField] private Image countDownGo;

    [SerializeField] private Transform[] spawnPoints;
    public GameObject[] obstacles;
    public List<GameObject> spawnedObstacles = new List<GameObject>();

    public NetworkManager networkManager;

    [SerializeField] private int scoreToWin = 3;
    [SerializeField] private float delayBeforeTitleScreen = 2f;
    [SerializeField] private string titleSceneName = "TitleScene";
    public int player1Score = 0;
    public int player2Score = 0;
    private int currentRound = 0;
    private bool gameOver = false;



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

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        countDown3.gameObject.SetActive(false);
        countDown2.gameObject.SetActive(false);

        countDown1.gameObject.SetActive(false);
        countDownGo.gameObject.SetActive(false);
        if (finalWinImage) finalWinImage.gameObject.SetActive(false); // Match win
        if (finalLoseImage) finalLoseImage.gameObject.SetActive(false); // Match lose
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleRoundReset(playerState deadPlayer)
    {
        if (!IsServer || gameOver) return;
        Debug.Log("about to run round reset");
        playerState[] players = FindObjectsOfType<playerState>();
        playerState winner = null;
        foreach (playerState player in players)
        {
            player.IsAlive.Value = false;
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
        if (!gameOver) // Only reset round if game isn't over
        {
            StartCoroutine(DelayedReset());
        }
    }

    private IEnumerator DelayedReset()
    {
        yield return new WaitForSeconds(4f);
        ResetRound();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseScore(playerState winner)
    {
        if (!IsServer || gameOver) return;
        bool gameShouldEnd = false;
        int winningClientId = -1;

        if (winner.Owner.ClientId == 0)
        {
            player1Score++;
            if (player1Score >= scoreToWin)
            {
                gameShouldEnd = true;
                winningClientId = 0;
            }
        }
        else
        {
            player2Score++;
            if (player2Score >= scoreToWin)
            {
                gameShouldEnd = true;
                winningClientId = 1;
            }
        }
        ChangePlayerScores(player1Score, player2Score);
        Debug.Log($"Player 1: {player1Score} Player 2: {player2Score}");

        if (gameShouldEnd)
        {
            Debug.Log($"Game Over. ClientId {winningClientId} wins.");
            gameOver = true;
            RpcHandleGameOver(winningClientId);
        }
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
        else if (score1 >= 3)
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
        else if (score2 >= 3)
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
    [ObserversRpc]
    private void RpcHandleGameOver(int winningClientId)
    {
        gameOver = true;
        if (winImage) winImage.gameObject.SetActive(false);
        if (loseImage) loseImage.gameObject.SetActive(false);
        if (NetworkManager.ClientManager.Connection.ClientId == winningClientId)
        {
            finalWinImage.gameObject.SetActive(true);
            finalLoseImage.gameObject.SetActive(false);
        }
        else
        {
            finalWinImage.gameObject.SetActive(false);
            finalLoseImage.gameObject.SetActive(true);
        }
        StartCoroutine(DelayedReturnToTitle());
    }

    private IEnumerator DelayedReturnToTitle()
    {
        yield return new WaitForSeconds(delayBeforeTitleScreen);

        if (finalWinImage) finalWinImage.gameObject.SetActive(false);
        if (finalLoseImage) finalLoseImage.gameObject.SetActive(false);

        if (IsClient)
        {
            ClientManager.StopConnection();
        }
        if (IsServer)
        {
            ServerManager.StopConnection(true);
            player1Score = 0;
            player2Score = 0;
            currentRound = 0;
            gameOver = false;
        }
        SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
        if (steamLobby != null)
        {
            steamLobby.LeaveCurrentLobby(); // This also stops FishNet connections
        }
        else
        {
            Debug.LogWarning("SteamLobby instance not found when trying to return to title.");
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }


    [ServerRpc(RequireOwnership = false)]
    private void ResetRound()
    {
        if (!IsServer || gameOver) return;
        currentRound++;
        removeWinImagery();
        Debug.Log($"Round {currentRound} starting...");
        playerState[] players = FindObjectsOfType<playerState>();
        DestroyPreviousObstacles();
        RandomizeObjects();
        StartCoroutine(StartCountdown());
        for (int i = 0; i < players.Length; i++)
        {
            players[i].RpcResetPlayerVisualsAndPosition(spawnPoints[i].position);
        }
    }
    private IEnumerator StartCountdown()
    {
        for (int i = 3; i > 0; i--)
        {
            Debug.Log("Countdown: " + i);
            if (i == 3)
            {
                DisplayCountdown(3);
                yield return new WaitForSeconds(1f);
            }
            else if (i == 2)
            {
                DisplayCountdown(2);
                yield return new WaitForSeconds(1f);
            }
            else if (i == 1)
            {
                DisplayCountdown(1);
                yield return new WaitForSeconds(1f);
            }
        }
        DisplayCountdown(0);
        yield return new WaitForSeconds(1f);
        DisplayCountdown(4);
    }

    [ObserversRpc]
    private void DisplayCountdown(int countdownValue)
    {
        switch (countdownValue)
        {
            case 3:
                countDown3.gameObject.SetActive(true);
                break;
            case 2:
                countDown2.gameObject.SetActive(true);
                countDown3.gameObject.SetActive(false);
                break;
            case 1:
                countDown1.gameObject.SetActive(true);
                countDown2.gameObject.SetActive(false);
                break;
            case 0:
                countDownGo.gameObject.SetActive(true);
                countDown1.gameObject.SetActive(false);
                break;
            case 4:
                countDownGo.gameObject.SetActive(false);
                break;
        }
    }

    [ObserversRpc]
    private void removeWinImagery()
    {
        winImage.gameObject.SetActive(false);
        loseImage.gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RandomizeObjects()
    {
        if (!IsServer) return;

        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();


        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-30, 30), 0, Random.Range(-30, -5));
            Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            positions.Add(randomPos);
            rotations.Add(randomRot);
        }

        for (int i = 0; i < 4; i++)
        {
            Vector3 mirroredPos = new Vector3(positions[i].x, positions[i].y, -positions[i].z);
            Quaternion mirroredRot = new Quaternion(rotations[i].x, rotations[i].y + 180, rotations[i].z, rotations[i].w);
            positions.Add(mirroredPos);
            rotations.Add(mirroredRot);
        }

        for (int j = 0; j < positions.Count; j++)
        {

            Debug.Log("random pos: " + positions[j].ToString());
            GameObject obstacleInstance;
            if (j < 4)
            {
                obstacleInstance = Instantiate(obstacles[Random.Range(0, obstacles.Length)], positions[j], rotations[j]);
            }
            else
            {
                obstacleInstance = Instantiate(obstacles[j % obstacles.Length], positions[j], rotations[j]);
            }
            // **Spawn the object on the network**
            ServerManager.Spawn(obstacleInstance);
            spawnedObstacles.Add(obstacleInstance);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyPreviousObstacles()
    {
        if (!IsServer) return;
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        spawnedObstacles.Clear();
    }
}
