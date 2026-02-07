using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float cooldown = 0.3f;

    private float lastShootTime = -999f;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (Time.time - lastShootTime >= cooldown)
        {
            Vector3 spawnPosition = shootPoint != null ? shootPoint.position : transform.position;
            Quaternion spawnRotation = shootPoint != null ? shootPoint.rotation : transform.rotation;

            Instantiate(projectilePrefab, spawnPosition, spawnRotation);
            lastShootTime = Time.time;
        }
    }
}
