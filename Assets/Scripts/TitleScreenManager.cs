using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Data;
using FishNet.Managing;
using FishNet.Managing.Scened;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private string gameSceneName = "GameScene";

    [SerializeField] private NetworkManager networkManager;
    private SteamLobby steamLobby;
    void Start()
    {
        steamLobby = FindObjectOfType<SteamLobby>();
        if (steamLobby == null)
        {
            statusText.text = "Error: SteamLobby system not found!";
            Debug.LogError("SteamLobby not found in the scene.");
            playButton.interactable = false;
            return;
        }
        if (playButton) playButton.onClick.AddListener(OnPlayButtonClicked);

        SteamLobby.OnStatusUpdate += UpdateStatusText;
        SteamLobby.OnLobbyOperationFinished += HandleLobbyOperationFinished;
        SteamLobby.OnPlayerCountChanged += HandlePlayerCountChanged;
        SteamLobby.OnReadyToStartGame += StartGame;

        if (steamLobby.IsInLobby)
        {
            playButton.interactable = false;
            UpdateStatusText($"Currently in lobby. Waiting... ({steamLobby.CurrentLobbyPlayerCount}/{SteamLobby.MAX_PLAYERS})");
            HandlePlayerCountChanged(steamLobby.CurrentLobbyPlayerCount, SteamLobby.MAX_PLAYERS);
        }
    }

    private void OnDestroy()
    {
        if (playButton) playButton.onClick.RemoveListener(OnPlayButtonClicked);
        SteamLobby.OnStatusUpdate -= UpdateStatusText;
        SteamLobby.OnLobbyOperationFinished -= HandleLobbyOperationFinished;
        SteamLobby.OnPlayerCountChanged -= HandlePlayerCountChanged;
        SteamLobby.OnReadyToStartGame -= StartGame;
    }

    private void OnPlayButtonClicked()
    {
        if (steamLobby == null || !SteamManager.Initialized)
        {
            UpdateStatusText("Steam is not ready. Please ensure Steam is running.");
            return;
        }
        playButton.interactable = false;
        UpdateStatusText("Processing...");
        steamLobby.FindAndJoinOrCreateLobby();
    }

    private void UpdateStatusText(string message)
    {
        if (statusText)
        {
            statusText.text = message;
        }
        Debug.Log($"[TitleScreenStatus] {message}");
    }

    private void HandleLobbyOperationFinished(bool success)
    {
        if (success)
        {
            playButton.interactable = false;
        }
        else
        {
            playButton.interactable = true;
            UpdateStatusText("Failed to create or join lobby. Please try again.");
        }
    }

    private void HandlePlayerCountChanged(int currentPlayers, int maxPlayers)
    {
        if (steamLobby.IsInLobby)
        {
            if (currentPlayers >= maxPlayers)
            {
                UpdateStatusText($"Waiting for players... ({currentPlayers}/{maxPlayers})");
            }
            else if (currentPlayers == maxPlayers)
            {
                UpdateStatusText($"Lobby full! ({currentPlayers}/{maxPlayers}) Preparing to start...");
            }
        }

    }

    private void StartGame()
    {
        UpdateStatusText("Starting game! Loading scene...");


        if (steamLobby != null && steamLobby.IsHost)
        {
            Debug.Log("[TitleScreenManager] Host is initiating scene load to: " + gameSceneName);

            SceneLoadData sld = new SceneLoadData(gameSceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            networkManager.SceneManager.LoadGlobalScenes(sld);
        }
        else if (steamLobby != null && !steamLobby.IsHost)
        {
            Debug.Log("[TitleScreenManager] Client is waiting for server to load scene: " + gameSceneName);
        }
        else
        {
            Debug.LogError("[TitleScreenManager] Cannot start game: SteamLobby reference is missing or host status unknown.");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
