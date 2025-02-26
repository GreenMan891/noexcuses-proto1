using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private static Transform[] spawnPoints = new Transform[2];
    private static int playersRegistered;

    public static void SetSpawnPoints(Transform[] points) => spawnPoints = points;
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (IsServer)
        {
            SpawnPlayer(Owner);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer(NetworkConnection conn)
    {
        GameObject player = Instantiate(playerPrefab);
        Spawn(player, conn);

        playersRegistered++;
        if (playersRegistered == 2)
        {
           TeleportAllPlayers();
        }
    }

    private void TeleportAllPlayers() {
        if (!IsServer || spawnPoints == null || spawnPoints.Length < 2) return;

        NetworkObject[] players = FindObjectsOfType<NetworkObject>();
        for (int i = 0; i < Mathf.Min(players.Length, spawnPoints.Length); i++)
        {
            players[i].transform.position = spawnPoints[i].position;
            players[i].transform.rotation = spawnPoints[i].rotation;
        }
    }
}
