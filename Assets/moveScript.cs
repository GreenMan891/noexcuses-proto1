using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveScript : MonoBehaviour
{

    private float xVelocity;
    private float zVelocity;
    public float moveSpeed = 10f;
    public float jumpForce = 5f;
    Rigidbody rb;
    public Camera cam;
    public float lookSpeed = 2f;
    private Vector3 cameraRot;
    private Vector3 playerRot;
    public float groundDistance = 2f;  // Distance to check for ground
    public LayerMask groundMask;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        movePlayer();
        moveMouse();
        Jump();
    }

    void moveMouse()
    {
        cameraRot = cam.transform.rotation.eulerAngles;
        cameraRot.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        cameraRot.x = Mathf.Clamp((cameraRot.x <= 180) ? cameraRot.x : -(360 - cameraRot.x), -80f, 80f);
        cam.transform.rotation = Quaternion.Euler(cameraRot);
        playerRot.y = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(playerRot);
    }


    void movePlayer()
    {
        xVelocity = Input.GetAxis("Horizontal");
        zVelocity = Input.GetAxis("Vertical");



        // Create an input vector in local space
        Vector3 input = new Vector3(xVelocity, 0, zVelocity);

        if (input == Vector3.zero)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // Get the camera's forward, but only in the horizontal plane
        Vector3 forward = cam.transform.forward;
        forward.y = 0;
        forward.Normalize();

        // Similarly, get the camera's right vector (projected onto the horizontal plane)
        Vector3 right = cam.transform.right;
        right.y = 0;
        right.Normalize();

        // Calculate the desired movement direction relative to the camera view
        Vector3 moveDir = (forward * input.z + right * input.x).normalized;

        // Apply the movement, preserving the current vertical velocity
        rb.velocity = moveDir * moveSpeed + new Vector3(0, rb.velocity.y, 0);
    }

    void Jump()
    {
        // Check if the player is grounded using a raycast from the player's position downward
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.1f, groundMask);

        // For visual debugging, uncomment the line below to see the ray in the scene view
        Debug.DrawRay(transform.position, Vector3.down * (groundDistance + 0.1f), Color.red);

        // Jump when space is pressed and the player is grounded
        if (Input.GetButton("Jump") && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
    }


}
