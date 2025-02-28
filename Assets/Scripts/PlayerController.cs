using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    private int playersSpawned = 0;

    public override void OnStartClient()
    {
        base.OnStartNetwork();
        SpawnPlayer(Owner);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer(NetworkConnection client = null)
    {
        GameObject player = Instantiate(playerPrefab, spawnPoints[playersSpawned].position, Quaternion.identity);
        Spawn(player, client);
        playersSpawned++;

        GameManager.Instance.RegisterPlayer(player);
    }

    // New method to move the client/player to a specified spawn point
    [ServerRpc(RequireOwnership = false)]
    public void TeleportToPositionServerRpc(GameObject player, Vector3 pos)
    {

        // Move the player's transform to the chosen spawn point.
        player.transform.position = pos;
    }
}