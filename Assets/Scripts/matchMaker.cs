using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;
using TMPro;
using Object = UnityEngine.Object;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Services.Relay.Models;

public class matchMaker : MonoBehaviour
{
    public TextMeshProUGUI updateText;

    private static UnityTransport _transport;
    public static String PlayerID { get; private set; }
    public int maxPlayers = 2;
    public int level = 1;
    public string lobbyID;
    public string lobbyName = "Name";

    public const string joinKey = "j";
    // Start is called before the first frame update
  
    public void Host() {
        NetworkManager.Singleton.StartHost();
        updateText.text = "Currently: Host";
    }
    public void Join() {
        NetworkManager.Singleton.StartClient();
        updateText.text = "Currently: Client";
    }


    public async void Play() {
        updateText.text = "Logging in";
        _transport = Object.FindObjectOfType<UnityTransport>();

        await Login();

        //CreateLobby();

        CheckLobbies();
    }

    public static async Task Login() {
        if (UnityServices.State == ServicesInitializationState.Uninitialized) {
            var options = new InitializationOptions();

            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn) {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerID = AuthenticationService.Instance.PlayerId;
        }
    }

    public async void CreateLobby() {
        updateText.text = "Creating Lobby";
        try {
            var a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

            var options = new CreateLobbyOptions {

                Data = new Dictionary<string, DataObject> {
                    {joinKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode)}
                }
            };

            var lobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
            lobbyID = lobby.Id;
            StartCoroutine(HeartBeat(lobbyID, 15));

            NetworkManager.Singleton.StartHost();
            updateText.text = "Lobby Host";
        } catch (Exception E) {
            Debug.LogError(E);
            updateText.text = "Failed to create lobby";
        }
    }

    public static IEnumerator HeartBeat(string lobbyID, float waitTimeSeconds) {
        var delay = new WaitForSeconds(waitTimeSeconds);
        while(true) {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyID);
            print("Beat");
            yield return delay;
        }
    }

    private void OnDestroy()
    {
        try{
            StopAllCoroutines();

        } catch  {
            Debug.LogFormat("Failed to destroy lobby");
        }
    }

    public async void CheckLobbies() {
        updateText.text = "Checking Lobbies";
        var queryOptions = new QueryLobbiesOptions {
            Filters = new List<QueryFilter> {
                new QueryFilter (
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    value:"0",
                    op: QueryFilter.OpOptions.GT)
            }
        };

        var response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        var lobbies = response.Results;

        if (lobbies.Count > 0) {
            foreach (var lobby in lobbies) {
                Debug.Log("lobby found!");
                JoinLobby(lobby);
            }

        } else {
            CreateLobby();
        }
    }

    public async void JoinLobby(Lobby lobby) {
        var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[joinKey].Value);
        lobbyID = lobby.Id;

        SetTransformAsClient(a);
        NetworkManager.Singleton.StartClient();
        updateText.text = "Lobby Client";
    }

    public void SetTransformAsClient(JoinAllocation a) {
        _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }
}
