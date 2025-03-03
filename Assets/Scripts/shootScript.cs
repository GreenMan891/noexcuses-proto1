using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Numerics;

public class shootScript : NetworkBehaviour
{
    public Camera cam;
    RaycastHit bulletHit;
    public GameObject bullet;
    public float fireRate = 400f;
    private bool canFire = true;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && canFire)
        {
            canFire = false;
            StartCoroutine(ShootDelay());
            ShootServerRPC(cam.transform.position, cam.transform.forward, transform.root.gameObject);
        }


    }

    [ServerRpc]
    void ShootServerRPC(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, GameObject shooter)
    {
        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out bulletHit, 300f))
        {
            Debug.Log("Hit: " + bulletHit.collider.name);
            Debug.Log("Tag: " + bulletHit.collider.tag);

            Transform hitRoot = bulletHit.collider.transform.root;
            if (hitRoot.gameObject == shooter)
            {
                Debug.Log("You hit yourself? How?");
                return;
            }

            playerState hitPlayerState = bulletHit.collider.transform.root.GetComponent<playerState>();
            if (bulletHit.collider.CompareTag("Player") || bulletHit.collider.CompareTag("Head"))
            {
                hitPlayerState.GetHit();
            }
        }
    }

    IEnumerator ShootDelay()
    {
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }
}
