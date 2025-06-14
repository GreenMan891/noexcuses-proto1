using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject titleScreen;

    public GameManager gameManager;
    private int playersSpawned = 0;

    public override void OnStartClient()
    {
        Debug.Log("Client started!!!!");
        base.OnStartNetwork();
        Debug.Log("Client started");

        StartCoroutine(WaitAndSpawnPlayer(Owner));
        StartCoroutine(WaitAndRandomizeObjects());
    }

    private IEnumerator WaitAndRandomizeObjects()
    {
        yield return new WaitForSeconds(1f);
        gameManager.RandomizeObjects();
    }


    public void Start()
    {

    }

    private IEnumerator WaitAndSpawnPlayer(NetworkConnection Owner = null)
    {
        yield return null;
        SpawnPlayer(Owner);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayer(NetworkConnection client = null)
    {
        GameObject player = Instantiate(playerPrefab, spawnPoints[playersSpawned].position, Quaternion.identity);
        Spawn(player, client);
        playersSpawned++;
    }
}


