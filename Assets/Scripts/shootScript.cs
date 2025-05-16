using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object; // Keep FishNet namespace

public class shootScript : NetworkBehaviour
{
    public Camera cam;
    // RaycastHit bulletHit; // Server-side hit info - now local to ServerRpc

    [Header("Shooting")]
    public float fireRate = 0.1f; // Seconds between shots
    private float nextFireTime = 0f;
    private float raycastDistance = 300f;

    [Header("Visuals")]
    public GameObject linePrefab; // Assign your BulletTrail prefab here
    // public float debugLineDuration = 1.0f; // Not needed for Line Renderer method

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

            // Optional: Client-side prediction DrawLine (Scene View only)
            // DrawClientPredictionLine(origin, direction);

            // Tell the server to perform the actual shot
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
                // PlayOneShot is great for sound effects.
                // It allows multiple sounds to overlap without cutting each other off
                // and you can specify the clip directly.
                audioSource.pitch = Random.Range(0.9f, 1.1f); // Vary pitch slightly between 0.9 and 1.1
                audioSource.PlayOneShot(shootSound);

                // You can also add a volume scale here if needed:
                // audioSource.PlayOneShot(gunshotSound, 0.8f); // Plays at 80% of the AudioSource's volume
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

    // Optional helper for scene-view prediction line
    // void DrawClientPredictionLine(Vector3 origin, Vector3 direction)
    // {
    //     Vector3 endPoint;
    //     if (Physics.Raycast(origin, direction, out RaycastHit clientHit, raycastDistance))
    //     {
    //         endPoint = clientHit.point;
    //     }
    //     else
    //     {
    //         endPoint = origin + direction * raycastDistance;
    //     }
    //     Debug.DrawLine(origin, endPoint, Color.green, 0.5f); // Short duration scene view line
    // }


    [ServerRpc(RequireOwnership = true)]
    void ShootServerRPC(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, GameObject shooter)
    {

        Vector3 serverEndPoint;
        RaycastHit bulletHit; // Keep hit info local to server method

        Vector3 adjustedOrigin = origin + direction * 0.5f; // Move the origin 0.3 units forward and 0.3 units to the right
        Ray ray = new Ray(adjustedOrigin, direction);
        bool didHit = Physics.Raycast(ray, out bulletHit, raycastDistance);

        if (didHit)
        {
            serverEndPoint = bulletHit.point; // End line at hit point

            // --- Server-Side Hit Logic ---
            Debug.Log($"[SERVER] Hit: {bulletHit.collider.name} at {bulletHit.point}");

            Transform hitRoot = bulletHit.collider.transform.root;
            Debug.Log($"[SERVER] Hit Root: {hitRoot.name}");
            if (hitRoot.gameObject == shooter)
            {
                Debug.Log("[SERVER] Hit self detected.");
                // Don't process damage, but still draw the line
            }
            // Check if the hit object has a playerState component only if not self
            else if (hitRoot.TryGetComponent<playerState>(out playerState hitPlayerState))
            {
                if (bulletHit.collider.CompareTag("Player") || bulletHit.collider.CompareTag("Head"))
                {
                    //play hit sound
                    if (audioSource != null && hitSound != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                    Debug.Log($"[SERVER] Dealing damage to {hitRoot.name}");
                    hitPlayerState.GetHit(); // Apply damage/effect
                }
            }
            // --- End Server-Side Hit Logic ---
        }
        else
        {
            // Raycast missed on the server
            serverEndPoint = origin + direction * raycastDistance; // End line at max distance
            Debug.Log("[SERVER] Missed (Max Distance)");
        }

        // Tell all clients to draw the line based on server's calculation
        DrawLineObserversRpc(origin, serverEndPoint);
    }

    [ObserversRpc(BufferLast = false)] // Send to all observers, don't buffer
    void DrawLineObserversRpc(UnityEngine.Vector3 start, UnityEngine.Vector3 end)
    {
        // if (linePrefab == null)
        // {
        //     Debug.LogError("Line Prefab is not assigned on shootScript!");
        //     return;
        // }

        TrailRenderer trail = Instantiate(bulletTrail, start, Quaternion.identity);
        StartCoroutine(SpawnTrail(trail, start, end));

        // Instantiate the prefab
        GameObject lineObj = Instantiate(linePrefab); // Instantiate at world origin, doesn't matter due to World Space

        // // Get the Line Renderer
        // LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        // if (lr == null)
        // {
        //     Debug.LogError("Line Prefab does not have a Line Renderer component!");
        //     Destroy(lineObj); // Clean up wrongly configured prefab instance
        //     return;
        // }

        // // Set the positions
        // lr.positionCount = 2; // Ensure position count is 2
        // lr.SetPosition(0, start);
        // lr.SetPosition(1, end);

        // The DestroyAfterTime script on the prefab will handle destroying the line object
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