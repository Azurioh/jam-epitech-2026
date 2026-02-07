using UnityEngine;
using UnityEngine.InputSystem;

public class FireballSpawner : MonoBehaviour
{
    public GameObject fireballPrefab;
    public float cooldown = 0.5f;
    private float lastFireTime = -999f;

    void Update()
    {
        // Spawn avec la touche F
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            SpawnFireball();
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SpawnFireball();
        }
    }

    void SpawnFireball()
    {
        if (Time.time - lastFireTime >= cooldown)
        {
            Instantiate(fireballPrefab, transform.position, transform.rotation);
            lastFireTime = Time.time;
        }
    }
}