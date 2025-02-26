using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

public class gameManager : NetworkBehaviour
{
    // Start is called before the first frame update

    public static gameManager Instance;
    public int player1Score = 0;
    public int player2Score = 0;

    private List<GameObject> players = new List<GameObject>();
    public GameObject player1;
    public GameObject player2;
    public int firstTo = 5;

    public float arenaSize = 45f;
    public float playerHeight = 1.5f;

    public bool gameStarted = false;
    public bool waitingForPlayers = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        // Only the host should run the game setup logic.
        if (IsHost)
        {
            if (!waitingForPlayers)
            {
                waitingForPlayers = true;
                Debug.Log("STARTING WAIT FOR PLAYERS COROUTINE");
                StartCoroutine(WaitForPlayersAndAssignPositions());
            }
            // Start a coroutine that waits until there are at least 2 fully spawned players.

            // Also subscribe in case players connect later.
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // When a new client connects, check if there are now at least 2 players.
        Debug.Log("New client connected.");
        Debug.Log("current clients: " + NetworkManager.Singleton.ConnectedClientsList.Count.ToString());
        Debug.Log("waiting for players: " + waitingForPlayers.ToString());
        // if (!gameStarted)
        //     StartCoroutine(WaitForPlayersAndAssignPositions());
        if (!gameStarted && !waitingForPlayers)
        {
            waitingForPlayers = true;
            Debug.Log("Starting coroutine to wait for players IN CLIENT CONNECTED.");
            StartCoroutine(WaitForPlayersAndAssignPositions());
            //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private IEnumerator WaitForPlayersAndAssignPositions()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || AllPlayersSpawned()) ;
        {
            yield return null;
        }
        Debug.Log("All players have spawned.");
        // Once both players are confirmed to be spawned, assign positions.
        AssignPlayerPositions();
        // Unsubscribe after assignment so this only happens once.
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private bool AllPlayersSpawned()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!client.PlayerObject.IsSpawned)
                return false;
        }
        return true;
    }

    private IEnumerator AssignPlayerPositions()
    {
        if (gameStarted)
        {
            Debug.LogWarning("Game has already started.");
            yield break;
        }
        gameStarted = true;
        Debug.Log("Host is assigning player positions...");


        // Get all players that have spawned
        NetworkManager networkManager = NetworkManager.Singleton;
        var players = networkManager.ConnectedClientsList;

        float angle = Random.Range(0f, Mathf.PI * 2); // Random angle in radians

        Vector3 position1 = new Vector3(
            arenaSize * Mathf.Cos(angle),
            playerHeight,
            arenaSize * Mathf.Sin(angle)
        );

        Vector3 position2 = new Vector3(
            arenaSize * Mathf.Cos(angle + Mathf.PI),
            playerHeight,
            arenaSize * Mathf.Sin(angle + Mathf.PI)
        );

        // Assign positions to the first two players
        int index = 0;
        foreach (var client in players)
        {
            GameObject playerObject = client.PlayerObject.gameObject;
            var networkObject = playerObject.GetComponent<NetworkObject>();

            // Temporarily assign ownership to the host
            if (!IsHost)
            {
                //networkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
            }

            if (index == 0)
            {
                playerObject.transform.position = new Vector3(-13, 2, 0);
                playerObject.transform.rotation = Quaternion.LookRotation(position2 - position1);
            }
            else if (index == 1)
            {
                playerObject.transform.position = new Vector3(58, 2, 0);
                playerObject.transform.rotation = Quaternion.LookRotation(position1 - position2);
            }

            //yield return new WaitForSeconds(0.1f);

            //networkObject.ChangeOwnership(client.ClientId);

            index++;
            if (index >= 2) break;
        }

        Debug.Log("Players have been positioned.");
    }
}
