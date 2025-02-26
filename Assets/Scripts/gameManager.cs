using System.Collections;
using System.Collections.Generic;
using FishNet.Example.Scened;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    
    private void Awake()
    {
        PlayerController.SetSpawnPoints(spawnPoints);
    }
}
