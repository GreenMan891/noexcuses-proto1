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

    private string MY_GAME_ID = "noExcusesGame";
    private bool isHost;
    private List<CSteamID> foundLobbies = new List<CSteamID>();
    public const int MAX_PLAYERS = 2;

    public static event Action<string> OnStatusUpdate;
    public static event Action<bool> OnLobbyOperationFinished; 
    public static event Action<int, int> OnPlayerCountChanged; 
    public static event Action OnReadyToStartGame; 

    public bool IsInLobby => currentLobbyID != CSteamID.Nil && currentLobbyID.IsValid();
    public bool IsHost => isHost;
    public int CurrentLobbyPlayerCount { get; private set; } = 0;

    private bool steamLobbyCoreInitialized = false;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyMatchList;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate; 

  
    void Awake()
    {
        Debug.Log("[SteamLobby] Awake STARTING.");
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitializeSteamSystems());
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

        steamLobbyCoreInitialized = true;
        Debug.Log("[SteamLobby] Core systems initialized and callbacks registered.");
    }

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

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50); 

        SteamMatchmaking.RequestLobbyList();


        
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameID", MY_GAME_ID, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();


    }
    public void CreateSteamLobby()
    {
        OnStatusUpdate?.Invoke("No suitable lobbies found. Creating a new lobby...");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MAX_PLAYERS); 
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
            CSteamID lobbyToJoin = foundLobbies[0]; 
            OnStatusUpdate?.Invoke($"Attempting to join lobby: {lobbyToJoin}...");
            SteamMatchmaking.JoinLobby(lobbyToJoin);
        }
        else
        {
            CreateSteamLobby();
        }
    }

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
        SteamMatchmaking.SetLobbyData(currentLobbyID, "GameID", MY_GAME_ID); 
        SteamMatchmaking.SetLobbyData(currentLobbyID, "name", $"{SteamFriends.GetPersonaName()}'s Lobby");
        SteamMatchmaking.SetLobbyJoinable(currentLobbyID, true);

        string actualName = SteamMatchmaking.GetLobbyData(currentLobbyID, "name");
        string actualGameId = SteamMatchmaking.GetLobbyData(currentLobbyID, "GameID");
        Debug.LogWarning($"[SteamLobby HOST DEBUG] Lobby Created. Retrieved Data - Name: '{actualName}', GameID: '{actualGameId}'. Expected GameID: 'noExcusesGame'");


        isHost = true;

        networkManager.ServerManager.StartConnection();
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

        if (!isHost)
        {
            string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, "HostAddress");
            if (string.IsNullOrEmpty(hostAddress))
            {
                Debug.LogError("Could not get HostAddress from lobby data. Cannot connect.");
                OnStatusUpdate?.Invoke("Error: Could not get host address.");
                LeaveCurrentLobby();
                return;
            }
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
        if (callback.m_ulSteamIDLobby == currentLobbyID.m_SteamID)
        {
            Debug.Log("Lobby data updated.");
        }
    }

    private void OnLobbyChatUpdated(LobbyChatUpdate_t callback)
    {

        if (callback.m_ulSteamIDLobby == currentLobbyID.m_SteamID)
        {
            Debug.Log($"Lobby chat update: User {callback.m_ulSteamIDUserChanged} status changed to {callback.m_rgfChatMemberStateChange}.");
            CurrentLobbyPlayerCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
            OnPlayerCountChanged?.Invoke(CurrentLobbyPlayerCount, MAX_PLAYERS);

            if (isHost)
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
        }
        else
        {
            OnStatusUpdate?.Invoke($"Waiting for players ({CurrentLobbyPlayerCount}/{MAX_PLAYERS})...");
        }
    }




    public void LeaveCurrentLobby()
    {
        if (IsInLobby)
        {
            OnStatusUpdate?.Invoke($"Leaving lobby: {currentLobbyID}");
            Debug.Log($"Leaving lobby: {currentLobbyID}");
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil;
            isHost = false;
            CurrentLobbyPlayerCount = 0;
            OnPlayerCountChanged?.Invoke(0, MAX_PLAYERS); 
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
        OnLobbyOperationFinished?.Invoke(false); 
    }

    private void OnApplicationQuit()
    {
        LeaveCurrentLobby();
    }
}
