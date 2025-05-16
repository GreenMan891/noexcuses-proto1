using FishNet;
using FishNet.Object;
using UnityEngine;
using System.Collections;
using FishNet.Managing;
using Steamworks;

public class ClientServerScript : NetworkBehaviour
{
    private void Start()
    {
        StartCoroutine(TryStartAsClientThenHost());
    }

    private IEnumerator TryStartAsClientThenHost()
    {

        Debug.Log("Trying to connect as a client...");

        InstanceFinder.ClientManager.StartConnection();

        yield return new WaitForSeconds(3);

        if (!InstanceFinder.ClientManager.Connection.IsActive)
        {
            Debug.Log("No server found, starting as host...");

            InstanceFinder.ServerManager.StartConnection();

            InstanceFinder.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("Connected as a client.");
        }
    }
}
