using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using FishNet.Object.Prediction;
using UnityEngine;
using System.Runtime.InteropServices;

public class playerState : NetworkBehaviour
{
    public moveScript moveScript;

    public GameObject headModel;

    public Animator animator;
    public ParticleSystem headExplosionParticles;
    public GameObject bloodSplatDecalPrefab;
    public Color headColor = Color.red;
    public Color blueColor;
    public Color currentColor;
    public PlayerModelVisibility playerModelVisibility;
    public float deathCameraPanTime = 2f;
    public Vector3 deathcameraOffset = new Vector3(0, 3, -2);
    private CharacterController controller;
    public Camera playerCamera;

    //SYNCVARS
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<Color> characterBloodColor = new SyncVar<Color>(Color.red);


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(IsOwner);
        }
    }

    public void Start()
    {
        moveScript moveScript = GetComponent<moveScript>();
        controller = GetComponent<CharacterController>();
        if (headModel != null && headModel.GetComponent<Renderer>() != null)
        {
            currentColor = headModel.GetComponent<Renderer>().material.color;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetHit()
    {
        if (!IsAlive.Value) return;
        IsAlive.Value = false;
        moveScript.controller.enabled = false;
        animator.StopPlayback();
        HandleDeathEffects();
        Debug.Log($"{gameObject.name} is dead");
        GameManager.Instance.HandleRoundReset(this);
    }

    [ObserversRpc]
    void HandleDeathEffects()
    {
        Color colorToUse = characterBloodColor.Value;
        Debug.Log("IsOwner: " + IsOwner + " | Blood Color: " + colorToUse);
        StartCoroutine(DeathSequence(colorToUse));
    }

    IEnumerator DeathSequence(Color colorToUse)
    {
        if (IsOwner && playerCamera != null)
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        }
        if (controller != null) controller.enabled = false;
        if (headModel != null) headModel.SetActive(false);
        if (headExplosionParticles != null)
        {
            Debug.Log("Head explosion particles instantiated");
            Vector3 headPos = headModel != null ? headModel.transform.position : transform.position + Vector3.up * 1.6f; // Approx head
            ParticleSystem explosionInstance = Instantiate(headExplosionParticles, headPos, Quaternion.identity);
            var mainModule = explosionInstance.main;
            mainModule.startColor = colorToUse; // Set blood color
            Destroy(explosionInstance.gameObject, mainModule.duration + mainModule.startLifetime.constantMax);
        }
        SpawnBloodSplat(colorToUse);
        if (IsOwner)
        {
            GameObject deathCamObject = new GameObject("DeathCamera");
            Camera deathCam = deathCamObject.AddComponent<Camera>();
            deathCam.fieldOfView = playerCamera != null ? playerCamera.fieldOfView : 60f;
            Vector3 startCamPos = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up * 1.5f;
            Quaternion startCamRot = playerCamera != null ? playerCamera.transform.rotation : Quaternion.LookRotation(transform.forward);
            Vector3 endCamPosRelative = deathcameraOffset;
            Vector3 endCamPosWorld = transform.TransformPoint(endCamPosRelative);
            deathCamObject.transform.position = startCamPos;
            deathCamObject.transform.rotation = startCamRot;
            float timer = 0;
            while (timer < deathCameraPanTime)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1.0f, timer / deathCameraPanTime);
                endCamPosWorld = transform.TransformPoint(endCamPosRelative);
                deathCamObject.transform.position = Vector3.Lerp(startCamPos, endCamPosWorld, t);
                Vector3 lookAtPos = transform.position + Vector3.up * 1.5f;
                deathCamObject.transform.LookAt(lookAtPos);
                yield return null;
            }
            Destroy(deathCamObject, 2f);
        }
    }

    void SpawnBloodSplat(Color colorToUse)
    {
        if (bloodSplatDecalPrefab == null) return;
        int numSplatters = Random.Range(10, 15);
        float splatterRadius = 50f;
        Vector3 origin = headModel != null ? headModel.transform.position : transform.position + Vector3.up * 1.5f;
        for (int i = 0; i < numSplatters; i++)
        {
            Debug.Log("Spawning blood splat");
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Abs(randomDir.y) * 0.5f; // Ensure it's not too vertical
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("Default", "Environment");
            if (Physics.Raycast(origin, randomDir, out hit, splatterRadius, layerMask))
            {
                GameObject splatDecal = Instantiate(bloodSplatDecalPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(-hit.normal));
                splatDecal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
                splatDecal.transform.localScale *= Random.Range(2.5f, 3.5f);

                Renderer splatRenderer = splatDecal.GetComponent<Renderer>();
                if (splatRenderer != null)
                {
                    splatRenderer.material.color = colorToUse; // Set blood color
                }
                Destroy(splatDecal, 5f); // Destroy after 5 seconds
            }
        }
    }

    [ObserversRpc]
    public void RpcResetPlayerVisualsAndPosition(Vector3 spawnPoint)
    {
        Debug.Log(gameObject.name + " Owner:" + IsOwner + ". Executing RpcResetPlayerVisualsAndPosition.");

        IsAlive.Value = true;

        // 1. Reset Position (CharacterController needs to be temporarily disabled for direct transform.position set)
        // bool controllerWasEnabled = false;
        // if (controller != null)
        // {
        //     controllerWasEnabled = controller.enabled;
        //     controller.enabled = false;
        // }
        // transform.position = spawnPoint;
        // //transform.rotation = Quaternion.identity; // Or a default spawn rotation if needed
        // if (controller != null && controllerWasEnabled) // Only re-enable if it was enabled before
        // {
        //     controller.enabled = true;
        // }


        // 2. Reset Visuals
        if (headModel != null)
        {
            headModel.SetActive(true); // THIS IS THE FIX - runs on all clients
            Debug.Log(gameObject.name + " Head model SetActive(true) on client. Owner: " + IsOwner);
        }
        else
        {
            Debug.LogWarning(gameObject.name + " Head model is null on client. Owner: " + IsOwner);
        }

        // 3. Re-enable player's own FPS camera if they are the owner
        // if (IsOwner && playerCamera != null)
        // {
        //     playerCamera.gameObject.SetActive(true);
        //     // Re-enable its AudioListener if it was disabled
        //     if (playerCamera.TryGetComponent<AudioListener>(out var listener) && !listener.enabled)
        //     {
        //         listener.enabled = true;
        //     }
        // }
        // If moveScript itself has an enabled flag for input processing:
        // if (moveScript != null) moveScript.enabled = IsOwner;

        moveScript.ResetPlayer(spawnPoint);
    }


    // [ServerRpc(RequireOwnership = false)]
    // public void ResetPlayer(Vector3 spawnPoint)
    // {
    //     IsAlive.Value = true;
    //     headModel.SetActive(true);
    //     moveScript.ResetPlayer(spawnPoint);
    // }


}
