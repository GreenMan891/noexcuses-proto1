using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object; // Keep FishNet namespace

public class shootScript : NetworkBehaviour
{
    public Camera cam;

    [Header("Shooting")]
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;
    private float raycastDistance = 300f;

    [Header("Visuals")]
    public GameObject linePrefab;

    [SerializeField] private ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration = 0.3f;
    [SerializeField] private TrailRenderer bulletTrail;

    [Header("Sounds")]
    public AudioClip shootSound;

    public AudioClip hitSound;
    private AudioSource audioSource;

    public Animator animator;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            UnityEngine.Vector3 origin = cam.transform.position;
            UnityEngine.Vector3 direction = cam.transform.forward;

            animator.SetTrigger("Fire");
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (muzzleLight != null)
            {
                StartCoroutine(FlashLight());
            }
            if (audioSource != null && shootSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f); 
                audioSource.PlayOneShot(shootSound);

            }

            ShootServerRPC(origin, direction, transform.root.gameObject);
        }
    }

    IEnumerator FlashLight()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }



    [ServerRpc(RequireOwnership = true)]
    void ShootServerRPC(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, GameObject shooter)
    {

        Vector3 serverEndPoint;
        RaycastHit bulletHit; 

        Vector3 adjustedOrigin = origin + direction * 0.5f; 
        Ray ray = new Ray(adjustedOrigin, direction);
        bool didHit = Physics.Raycast(ray, out bulletHit, raycastDistance);

        if (didHit)
        {
            serverEndPoint = bulletHit.point; 


            Debug.Log($"[SERVER] Hit: {bulletHit.collider.name} at {bulletHit.point}");

            Transform hitRoot = bulletHit.collider.transform.root;
            Debug.Log($"[SERVER] Hit Root: {hitRoot.name}");
            if (hitRoot.gameObject == shooter)
            {
                Debug.Log("[SERVER] Hit self detected.");
            }
            else if (hitRoot.TryGetComponent<playerState>(out playerState hitPlayerState))
            {
                if (bulletHit.collider.CompareTag("Player") || bulletHit.collider.CompareTag("Head"))
                {
                    if (audioSource != null && hitSound != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                    Debug.Log($"[SERVER] Dealing damage to {hitRoot.name}");
                    hitPlayerState.GetHit();
                }
            }
        }
        else
        {
            serverEndPoint = origin + direction * raycastDistance; 
            Debug.Log("[SERVER] Missed (Max Distance)");
        }


        DrawLineObserversRpc(origin, serverEndPoint);
    }

    [ObserversRpc(BufferLast = false)] 
        void DrawLineObserversRpc(UnityEngine.Vector3 start, UnityEngine.Vector3 end)
    {
        TrailRenderer trail = Instantiate(bulletTrail, start, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, start, end));

        GameObject lineObj = Instantiate(linePrefab);
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 start, Vector3 end)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(start, end);
        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(start, end, 1 - (remainingDistance / distance));
            remainingDistance -= 100 * Time.deltaTime;
            yield return null;
        }
        trail.transform.position = end;
        Destroy(trail.gameObject, trail.time);
    }
}