using FishNet;
using UnityEngine;

public class ClientServerScript : MonoBehaviour
{
    [SerializeField] private bool isServer = true;
    
    private void Start()
    {
        if (isServer)
        {
            // Start as host (server + client)
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
        else
        {
            // Start as client only
            InstanceFinder.ClientManager.StartConnection();
        }
    }

    private void OnDestroy()
    {
        InstanceFinder.ServerManager.StopConnection(true);
        InstanceFinder.ClientManager.StopConnection();
    }
}