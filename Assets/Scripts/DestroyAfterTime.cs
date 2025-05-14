using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float lifetime = 0.5f; // How long the line stays visible

    void Start()
    {
        // Destroy the GameObject this script is attached to after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }
}