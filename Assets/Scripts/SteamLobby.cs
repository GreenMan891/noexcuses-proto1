using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using FishySteamworks;
using FishNet.Managing;
using Steamworks;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;


    private CSteamID currentLobbyID;

    private string MY_GAME_ID = "noExcusesGame"; // This should be unique to your game
    private bool isHost;
    private List<CSteamID> foundLobbies = new List<CSteamID>();
    public const int MAX_PLAYERS = 2;

    public static event Action<string> OnStatusUpdate; // For general status messages
    public static event Action<bool> OnLobbyOperationFinished; // True if successfully in a lobby, false if failed/no lobby
    public static event Action<int, int> OnPlayerCountChanged; // currentPlayers, maxPlayers
    public static event Action OnReadyToStartGame; // When 2 players are in

    public bool IsInLobby => currentLobbyID != CSteamID.Nil && currentLobbyID.IsValid();
    public bool IsHost => isHost;
    public int CurrentLobbyPlayerCount { get; private set; } = 0;

    private bool steamLobbyCoreInitialized = false;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyMatchList;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate; // For lobby metadata changes
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate; // For player join/leave





    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("[SteamLobby] Awake STARTING.");
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitializeSteamSystems());

        // hostButton.onClick.AddListener(() =>
        // {
        //     hostButton.gameObject.SetActive(false);
        //     joinButton.gameObject.SetActive(false);
        //     lobbyIdInput.gameObject.SetActive(false);
        //     CreateSteamLobby();
        // });
        // if (joinButton && lobbyIdInput) joinButton.onClick.AddListener(() => JoinSteamLobby(lobbyIdInput.text));
    }

    private IEnumerator InitializeSteamSystems()
    {
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            Debug.LogError("NetworkManager not assigned!");
            enabled = false;
        }
        Debug.Log($"[SteamLobby] NetworkManager found: {networkManager.name}");
        yield return null;
        yield return null;
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steamworks not initialized! Make sure Steam is running and steam_appid.txt is present.");
            enabled = false;
        }

        Debug.Log("[SteamLobby] SteamManager.Initialized is TRUE. Proceeding with SteamLobby setup.");
        OnStatusUpdate?.Invoke("Status: steam is ready.");
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdated);

        steamLobbyCoreInitialized = true; // Mark as initialized
        Debug.Log("[SteamLobby] Core systems initialized and callbacks registered.");
    }

    // Update is called once per frame
    void Update()
    {
        if (SteamManager.Initialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    public void FindAndJoinOrCreateLobby()
    {
        if (!steamLobbyCoreInitialized) { /* ... */ }
        if (!SteamManager.Initialized) { /* ... */ }

        OnStatusUpdate?.Invoke("Searching for available lobbies...");
        foundLobbies.Clear();

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50); // Limit to 50 results for safety

        SteamMatchmaking.RequestLobbyList();


        

        // ADD FILTERS FOR YOUR GAME
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameID", MY_GAME_ID, ELobbyComparison.k_ELobbyComparisonEqual);
        // SteamMatchmaking.AddRequestLobbyListNumericalFilter("slots_available", 1, ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan);
        // You might want to limit the number of results to avoid too many callbacks if many test lobbies exist
        // SteamMatchmaking.AddRequestLobbyListResultCountFilter(10); // Get up to 10 results
        SteamMatchmaking.RequestLobbyList();


    }
    public void CreateSteamLobby()
    {
        OnStatusUpdate?.Invoke("No suitable lobbies found. Creating a new lobby...");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MAX_PLAYERS); // Or k_ELobbyTypePublic, max members
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        OnStatusUpdate?.Invoke($"Found {callback.m_nLobbiesMatching} potential lobbies.");
        foundLobbies.Clear();

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            if (numMembers < MAX_PLAYERS)
            {
                foundLobbies.Add(lobbyID);
                Debug.Log($"Lobby {lobbyID} has {numMembers}/{MAX_PLAYERS} players. Suitable.");
            }
            else
            {
                Debug.Log($"Lobby {lobbyID} is full ({numMembers}/{MAX_PLAYERS}). Not suitable.");
            }
        }

        if (foundLobbies.Count > 0)
        {
            // Join the first suitable lobby found
            CSteamID lobbyToJoin = foundLobbies[0]; // Simplistic: join the first one
            OnStatusUpdate?.Invoke($"Attempting to join lobby: {lobbyToJoin}...");
            SteamMatchmaking.JoinLobby(lobbyToJoin);
        }
        else
        {
            CreateSteamLobby();
        }
    }



    // public void JoinSteamLobby(string lobbyIdString)
    // {
    //     if (string.IsNullOrEmpty(lobbyIdString))
    //     {
    //         Debug.LogWarning("Lobby ID input is empty.");
    //         return;
    //     }

    //     if (ulong.TryParse(lobbyIdString, out ulong lobbyIdUlong))
    //     {
    //         CSteamID lobbyID = new CSteamID(lobbyIdUlong);
    //         Debug.Log($"Joining lobby {lobbyID}...");
    //         SteamMatchmaking.JoinLobby(lobbyID);
    //     }
    //     else
    //     {
    //         Debug.LogError("Invalid Lobby ID format.");
    //     }
    // }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Lobby creation failed: {callback.m_eResult}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"Lobby created successfully! Lobby ID: {currentLobbyID}");
        OnStatusUpdate?.Invoke($"Lobby Created! Waiting for players...");

        SteamMatchmaking.SetLobbyData(currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyID, "name", $"{SteamFriends.GetPersonaName()}'s Game");
        SteamMatchmaking.SetLobbyData(currentLobbyID, "GameID", MY_GAME_ID); // ADD THIS
        SteamMatchmaking.SetLobbyData(currentLobbyID, "name", $"{SteamFriends.GetPersonaName()}'s Lobby");
        SteamMatchmaking.SetLobbyJoinable(currentLobbyID, true); // Make sure it's joinable

        string actualName = SteamMatchmaking.GetLobbyData(currentLobbyID, "name");
        string actualGameId = SteamMatchmaking.GetLobbyData(currentLobbyID, "GameID");
        Debug.LogWarning($"[SteamLobby HOST DEBUG] Lobby Created. Retrieved Data - Name: '{actualName}', GameID: '{actualGameId}'. Expected GameID: 'noExcusesGame'");


        isHost = true;

        // Start FishNet server
        networkManager.ServerManager.StartConnection();
        // Host also connects as a client
        // FishySteamworks will use the Steam P2P connection to the "HostAddress" (which is self)
        networkManager.ClientManager.StartConnection(SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Joining lobby from invite: {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string lobbyName = SteamMatchmaking.GetLobbyData(currentLobbyID, "name");
        OnStatusUpdate?.Invoke($"Joined lobby: {lobbyName} ({currentLobbyID})");
        Debug.Log($"Entered lobby: {currentLobbyID}. Name: {lobbyName}");

        CurrentLobbyPlayerCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        OnPlayerCountChanged?.Invoke(CurrentLobbyPlayerCount, MAX_PLAYERS);
        OnLobbyOperationFinished?.Invoke(true);

        if (!isHost) // If we are not the host, we are a client joining
        {
            // Get the host's SteamID from lobby data
            string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, "HostAddress");
            if (string.IsNullOrEmpty(hostAddress))
            {
                Debug.LogError("Could not get HostAddress from lobby data. Cannot connect.");
                OnStatusUpdate?.Invoke("Error: Could not get host address.");
                LeaveCurrentLobby();
                return;
            }
            // Start FishNet client, connecting to the host via their SteamID
            // FishySteamworks will handle translating this SteamID to a P2P connection.
            OnStatusUpdate?.Invoke("Connecting to host...");
            networkManager.ClientManager.StartConnection(hostAddress);
        }
        else
        {
            CheckPlayerCountAndReady();
        }
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        // This callback is triggered when lobby data is changed.
        // We can use this to react to custom game state changes if needed.
        // For player count, LobbyChatUpdate is more direct for join/leave.
        if (callback.m_ulSteamIDLobby == currentLobbyID.m_SteamID)
        {
            Debug.Log("Lobby data updated.");
            // Potentially refresh lobby info or player list if you display it
        }
    }

    private void OnLobbyChatUpdated(LobbyChatUpdate_t callback)
    {
        // This callback is triggered when a user joins, leaves, is kicked, or disconnected from the lobby.
        if (callback.m_ulSteamIDLobby == currentLobbyID.m_SteamID)
        {
            Debug.Log($"Lobby chat update: User {callback.m_ulSteamIDUserChanged} status changed to {callback.m_rgfChatMemberStateChange}.");
            CurrentLobbyPlayerCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
            OnPlayerCountChanged?.Invoke(CurrentLobbyPlayerCount, MAX_PLAYERS);

            if (isHost) // Only host decides when to start
            {
                CheckPlayerCountAndReady();
            }
        }
    }

    private void CheckPlayerCountAndReady()
    {
        if (CurrentLobbyPlayerCount == MAX_PLAYERS)
        {
            OnStatusUpdate?.Invoke("Lobby full! Starting game...");
            OnReadyToStartGame?.Invoke();
            // Optionally, make the lobby not joinable anymore if the game is starting
            // SteamMatchmaking.SetLobbyJoinable(currentLobbyID, false);
        }
        else
        {
            OnStatusUpdate?.Invoke($"Waiting for players ({CurrentLobbyPlayerCount}/{MAX_PLAYERS})...");
        }
    }



    // private void OnLobbyMatchList(LobbyMatchList_t pCallback)
    // {
    //     Debug.Log($"Found {pCallback.m_nLobbiesMatching} lobbies.");
    //     for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
    //     {
    //         CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
    //         string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
    //         Debug.Log($"Lobby {i}: ID={lobbyID}, Name='{lobbyName}'");
    //         // You would populate a UI list here
    //     }
    // }
    public void LeaveCurrentLobby()
    {
        if (IsInLobby)
        {
            OnStatusUpdate?.Invoke($"Leaving lobby: {currentLobbyID}");
            Debug.Log($"Leaving lobby: {currentLobbyID}");
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil; // Clear current lobby ID
            isHost = false;
            CurrentLobbyPlayerCount = 0;
            OnPlayerCountChanged?.Invoke(0, MAX_PLAYERS); // Update count
        }

        if (networkManager != null)
        {
            if (networkManager.ServerManager.Started)
            {
                networkManager.ServerManager.StopConnection(true);
            }
            if (networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StopConnection();
            }
        }
        OnLobbyOperationFinished?.Invoke(false); // Indicate we are no longer in a lobby ready state
    }

    private void OnApplicationQuit()
    {
        LeaveCurrentLobby();
    }
}
