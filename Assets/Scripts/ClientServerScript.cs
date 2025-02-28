using FishNet;
using FishNet.Object;
using UnityEngine;
using System.Collections;

public class ClientServerScript : NetworkBehaviour
{
    private void Start()
    {
        StartCoroutine(TryStartAsClientThenHost());
    }

    private IEnumerator TryStartAsClientThenHost()
    {
        // Try connecting as a client first
        Debug.Log("Trying to connect as a client...");
        InstanceFinder.ClientManager.StartConnection();

        // Wait a few seconds to check if the connection is successful
        yield return new WaitForSeconds(3);

        if (!InstanceFinder.ClientManager.Connection.IsActive)
        {
            Debug.Log("No server found, starting as host...");

            // Start as server
            InstanceFinder.ServerManager.StartConnection();

            // Start as client (host mode)
            InstanceFinder.ClientManager.StartConnection();
        }
        else
        {
            Debug.Log("Connected as a client.");
        }
    }
}
