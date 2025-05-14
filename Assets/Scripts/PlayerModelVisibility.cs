using UnityEngine;
using FishNet.Object;
using UnityEngine.Rendering;
using FishNet.Connection;
using Unity.VisualScripting;

public class PlayerModelVisibility : NetworkBehaviour
{
    public GameObject firstPersonArms;
    public Renderer[] thirdPersonModelRenderers;

    public Renderer headRenderer;

    public Material friendlyHeadMaterial;
    public Material enemyHeadMaterial;

    public playerState playerState;
    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        base.OnOwnershipClient(prevOwner);
        SetupModelVisibility();
        UpdateHeadColour();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHeadColour();
        //SetupModelVisibility();
    }

    public void UpdateHeadColour()
    {
        // Wait until the client manager and connection are ready
        if (!IsClient || ClientManager == null || !ClientManager.Started || ClientManager.Connection == null || ClientManager.Connection.FirstObject == null)
        {
            // Too early to get local player info, try again shortly
            // Invoke(nameof(UpdateHeadColor), 0.1f); // Or use a coroutine
            return;
        }
        headRenderer.material = IsOwner ? friendlyHeadMaterial : enemyHeadMaterial;
        playerState.characterBloodColor.Value = headRenderer.material.color;
    }

    private void SetupModelVisibility()
    {
        if (firstPersonArms == null || thirdPersonModelRenderers == null)
        {
            Debug.LogError("PlayerModelVisibility: Missing references to firstPersonArms or thirdPersonModelRenderer.");
            return;
        }
        if (IsOwner)
        {
            if (firstPersonArms.activeSelf == false)
            {
                firstPersonArms.SetActive(true);
            }
            foreach (Renderer thirdPersonModelRenderer in thirdPersonModelRenderers)
            {
                if (thirdPersonModelRenderer != null)
                {
                    try
                    {
                        Debug.Log($"Setting shadow casting mode to ShadowsOnly for: {thirdPersonModelRenderer.gameObject.name}");
                        thirdPersonModelRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    }
                    catch (System.Exception e)
                    {
                        thirdPersonModelRenderer.enabled = false;
                        Debug.LogError($"Failed to set shadow casting mode for {thirdPersonModelRenderer.gameObject.name}: {e.Message}");
                    }
                }
            }
        }
        else
        {
            if (firstPersonArms != null)
            {
                firstPersonArms.SetActive(false);
            }
            foreach (Renderer thirdPersonModelRenderer in thirdPersonModelRenderers)
            {
                if (thirdPersonModelRenderer != null)
                {
                    thirdPersonModelRenderer.shadowCastingMode = ShadowCastingMode.On;
                    thirdPersonModelRenderer.enabled = true;
                }
            }
        }
    }
}

