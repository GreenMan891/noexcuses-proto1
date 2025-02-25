using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shootScript : MonoBehaviour
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
        Shoot();
    }

    void Shoot() {
        if (Input.GetMouseButton(0) && canFire) {
            canFire = false;
            StartCoroutine(ShootDelay());
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            GameObject bulletInstance = Instantiate(bullet, cam.transform.position + transform.forward * 1.0f, Quaternion.identity);
            bulletInstance.transform.eulerAngles = new Vector3(90, transform.eulerAngles.y, transform.eulerAngles.z);
            bulletInstance.GetComponent<Rigidbody>().AddForce(ray.direction * 50000);

            if (Physics.Raycast(ray, out bulletHit, 300f, ~LayerMask.GetMask("Bullet"))) {
                Debug.Log("Hit: " + bulletHit.collider.name); 
                if (bulletHit.collider.tag == "Player") {
                    
                    Destroy(bulletHit.collider.gameObject);
                }
                // Additional logic if the raycast hits something
            }
        }
    }

    IEnumerator ShootDelay() {
        yield return new WaitForSeconds(fireRate);
        canFire = true;
    }
}
