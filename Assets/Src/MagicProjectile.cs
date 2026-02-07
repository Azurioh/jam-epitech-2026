using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
        
        // Vérifier/ajouter un Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // Kinematic car on contrôle le mouvement manuellement
            Debug.Log($"MagicProjectile: Added Rigidbody to {gameObject.name}");
        }
        
        DamageOnContact dmg = GetComponent<DamageOnContact>();
        if (dmg != null)
        {
            dmg.EnableHitbox();
            Debug.Log($"MagicProjectile: DamageOnContact enabled on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"MagicProjectile: No DamageOnContact found on {gameObject.name}");
        }

        // Vérifier qu'il y a un collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"MagicProjectile: No Collider found on {gameObject.name}!");
        }
        else
        {
            Debug.Log($"MagicProjectile: Collider found - isTrigger: {col.isTrigger}");
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
