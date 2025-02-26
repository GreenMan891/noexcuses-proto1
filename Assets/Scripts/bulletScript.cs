using UnityEngine;

public class Bullet : MonoBehaviour
{
    // This function is called when the bullet collides with another collider

    void Start()
    {
        Destroy(gameObject, 2f); // Destroy bullet after 5 seconds if it doesn't hit anything
    }
    void OnCollisionEnter(Collision collision)
    {
        // Optional: You can check what you hit by using collision.gameObject
        // For example, ignore collisions with the player or other bullets if needed

        // Destroy the bullet on collision
        Destroy(gameObject);
    }
}