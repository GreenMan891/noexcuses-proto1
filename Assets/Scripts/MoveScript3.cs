using FishNet.Object;
using Q3Movement;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MoveScript3 : NetworkBehaviour
{

    [Header("Camera Settings")]
    public Camera playCamera;           // Reference to the camera transform
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
    private CharacterController controller;

    void Awake()
    {
        cameraRotation = GetComponent<FirstPersonCameraRotation>();
        if (cameraRotation == null)
        {
            cameraRotation = gameObject.AddComponent<FirstPersonCameraRotation>();
        }
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
            cameraRotation.Init(transform, playCamera.transform);
        }
        else
        {
            gameObject.GetComponent<MoveScript3>().enabled = false; // Disable the move script for non-local players // Disable the camera rotation for non-local players
        }
    }
    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;


    }

    void Update()
    {
        // Update ground normal for slope handling
        UpdateGroundNormal();

        // Input
        float forward = Input.GetAxisRaw("Vertical");
        float strafe = Input.GetAxisRaw("Horizontal");
        bool wishJump = Input.GetButtonDown("Jump");

        Vector2 input = new Vector2(strafe, forward);
        if (input.magnitude > 1f)
            input.Normalize();

        // Desired movement
        Vector3 wishDir = transform.forward * input.y + transform.right * input.x;
        float wishSpeed = input.magnitude * maxGroundSpeed;

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

        if (playCamera != null)
        {
            cameraRotation.LookRotation(transform, playCamera.transform);

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
}
