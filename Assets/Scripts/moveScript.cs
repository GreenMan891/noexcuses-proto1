using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class moveScript : NetworkBehaviour
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
        if (!IsOwner)
        {
            cam.gameObject.SetActive(false);
            return;
        } else {
            cam.gameObject.SetActive(true);
        }
        movePlayer();
        Jump();
    }



    void LateUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        moveMouse();
    }

    public void switchCam()
    {
        if (IsOwner)
        {
            var waitingCam = GameObject.Find("MenuCamera");
            if (waitingCam != null)
            {
                waitingCam.SetActive(false);
            }
        }
    }

    void moveMouse()
    {
        cameraRot = cam.transform.rotation.eulerAngles;
        cameraRot.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        cameraRot.x = Mathf.Clamp((cameraRot.x <= 180) ? cameraRot.x : -(360 - cameraRot.x), -80f, 80f);
        cam.transform.rotation = Quaternion.Euler(cameraRot);

        playerRot.y += Input.GetAxis("Mouse X") * lookSpeed;
        transform.rotation = Quaternion.Euler(0, playerRot.y, 0);
    }


    void movePlayer()
    {
        if (Input.GetKey(KeyCode.W))
        {
            zVelocity = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            zVelocity = -1;
        }
        else
        {
            zVelocity = 0;
        }
        if (Input.GetKey(KeyCode.A))
        {
            xVelocity = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            xVelocity = 1;
        }
        else
        {
            xVelocity = 0;
        }


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
        Vector3 targetVelocity = moveDir * moveSpeed + new Vector3(0, rb.velocity.y, 0);
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 10f);
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

    public void TeleportPlayer(Vector3 position)
    {
        transform.position = position;
    }


}
