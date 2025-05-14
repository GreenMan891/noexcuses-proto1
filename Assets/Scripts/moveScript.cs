using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Q3Movement;
using UnityEngine;

public class moveScript : NetworkBehaviour
{
    public Transform orientation;
    public Camera cam;
    private FirstPersonCameraRotation cameraRotation;

    [Header("Movement Settings")]
    public float maxSpeed = 7.0f;
    public float maxGroundSpeed = 10.0f;         // Maximum speed on ground
    public float groundAcceleration = 10.0f;
    public float airAcceleration = 1.0f;
    public float airControl = 2.0f;           // Additional air control factor
    public float friction = 6.0f;
    public float gravity = 20.0f;
    public float jumpVelocity = 8.0f;

    [Header("Slope and Ground Detection")]
    public float slopeLimit = 45f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 groundNormal = Vector3.up;
    public CharacterController controller;

    public playerState playerState;
    private bool hasSwitchedCam = false;

    public Animator animator;

    float forward, strafe;
    bool wishJump = false;
    //[SerializeField] public GameObject titleScreen;

    void Awake()
    {
        cameraRotation = GetComponent<FirstPersonCameraRotation>();
        if (cameraRotation == null)
        {
            cameraRotation = gameObject.AddComponent<FirstPersonCameraRotation>();
        }
        playerState = GetComponent<playerState>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            // playCamera = Camera.main;
            // Debug.Log("Camera assigned to player: " + playCamera.name);
            // playCamera.transform.position = transform.position;
            // playCamera.transform.SetParent(transform);
            cameraRotation.Init(transform, cam.transform);
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            cam.gameObject.SetActive(false);
            return;
        }
        else
        {
            cam.gameObject.SetActive(true);
        }
        if (!playerState.IsAlive.Value && playerState != null) return;

        if (!hasSwitchedCam)
        {
            switchCam();
            //titleScreen.SetActive(false);
            hasSwitchedCam = true;

        }
        Movement(); 
        // movePlayer();
        // Jump();
    }

    void Movement() {
                // Update ground normal for slope handling
        UpdateGroundNormal();

        // Input
        float forward = Input.GetAxisRaw("Vertical");
        animator.SetFloat("ForwardInput", forward);
        float strafe = Input.GetAxisRaw("Horizontal");
        animator.SetFloat("SideInput", strafe);
        bool wishJump = Input.GetButtonDown("Jump");

        Vector2 input = new Vector2(strafe, forward);
        if (input.magnitude > 1f)
            input.Normalize();

        // Desired movement
        Vector3 wishDir = transform.forward * input.y + transform.right * input.x;
        float wishSpeed = input.magnitude * maxGroundSpeed;
        animator.SetFloat("YVelocity", velocity.y);
        animator.SetBool("grounded", controller.isGrounded);

        if (controller.isGrounded)
        {
            // Align movement to slope
            wishDir = Vector3.ProjectOnPlane(wishDir, groundNormal).normalized;

            // Apply ground friction
            ApplyFriction();

            // Accelerate on ground
            Accelerate(wishDir, wishSpeed, groundAcceleration);

            // Jump
            if (wishJump)
            {
                velocity.y = jumpVelocity;
                if (input == Vector2.zero)
                {
                    animator.SetTrigger("JumpIdle");
                }
                else
                {
                animator.SetTrigger("JumpStart");
                }
            }
            else if (velocity.y < 0)
            {
                // Keep a slight downward force to stick to ground
                velocity.y = -1f;
            }
        }
        else
        {
            // Air acceleration
            Accelerate(wishDir.normalized, wishSpeed, airAcceleration);

            // Air control tweak (Q3 style)
            AirControl(wishDir.normalized, wishSpeed);

            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;
        }

        // Move character
        controller.Move(velocity * Time.deltaTime);

        if (cam != null)
        {
            cameraRotation.LookRotation(transform, cam.transform);

        }
    }

    private void UpdateGroundNormal()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out hit,
            controller.height / 2 - controller.radius + groundCheckDistance, groundLayer))
        {
            groundNormal = hit.normal;
        }
        else
        {
            groundNormal = Vector3.up;
        }
    }

    /// <summary>
    /// Applies friction to horizontal velocity.
    /// </summary>
    private void ApplyFriction()
    {
        Vector3 horizVel = new Vector3(velocity.x, 0, velocity.z);
        float speed = horizVel.magnitude;
        if (speed < 0.001f) return;

        float drop = speed * friction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        horizVel *= newSpeed / speed;

        velocity.x = horizVel.x;
        velocity.z = horizVel.z;
    }

    /// <summary>
    /// Standard Q3 acceleration.
    /// </summary>
    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        float currentSpeed = Vector3.Dot(velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0) return;

        float accelSpeed = accel * Time.deltaTime * wishSpeed;
        accelSpeed = Mathf.Min(accelSpeed, addSpeed);

        velocity += accelSpeed * wishDir;
    }

    /// <summary>
    /// Adds additional air control when airborne, based on Q3 behavior.
    /// </summary>
    private void AirControl(Vector3 wishDir, float wishSpeed)
    {
        if (Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.001f || wishSpeed < 0.001f) return;

        float zVel = velocity.y;
        Vector3 vel = new Vector3(velocity.x, 0, velocity.z);
        float speed = vel.magnitude;
        vel.Normalize();

        float dot = Vector3.Dot(vel, wishDir);
        float k = airControl * dot * dot * Time.deltaTime;

        if (dot > 0)
        {
            vel = vel * speed + wishDir * k;
            vel.Normalize();
            vel *= speed;

            velocity.x = vel.x;
            velocity.z = vel.z;
            velocity.y = zVel;
        }
    }

    public void switchCam()
    {
        Debug.Log("Switching Camera");
        if (IsOwner)
        {
            var waitingCam = GameObject.Find("MenuCamera");
            if (waitingCam != null)
            {
                waitingCam.SetActive(false);
            }
        }
        else
        {
            Debug.Log("Not Owner");
        }
    }

    [ObserversRpc]
    public void ResetPlayer(Vector3 spawnPoint)
    {
        transform.position = spawnPoint;
        velocity = Vector3.zero;
        StartCoroutine(WaitAndEnableController(spawnPoint));
    }

    private IEnumerator WaitAndEnableController(Vector3 spawnPoint)
    {
        controller.enabled = false;
        yield return new WaitForSeconds(3f);
        controller.enabled = true;
        transform.position = spawnPoint;
    }

    [ObserversRpc]
    public void ResetLocation(Vector3 spawnPoint)
    {
        transform.position = spawnPoint;
    }


}
