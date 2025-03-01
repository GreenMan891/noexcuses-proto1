using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using FishNet.Object.Prediction;
using UnityEngine;

public class playerState : NetworkBehaviour
{
    public moveScript moveScript;
    //SYNCVARS
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    public void Start()
    {
        moveScript moveScript = GetComponent<moveScript>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetHit()
    {
        if (!IsAlive.Value) return;
        IsAlive.Value = false;
        Debug.Log($"{gameObject.name} is dead");
        GameManager.Instance.HandleRoundReset(this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetPlayer(Vector3 spawnPoint)
    {
        IsAlive.Value = true;
        moveScript.ResetPlayer(spawnPoint);
    }


}
