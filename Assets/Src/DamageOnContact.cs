using System.Collections.Generic;
using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool destroyOnHit;
    [SerializeField] private float hitCooldown = 0.5f;

    private Collider hitbox;
    private Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    void Awake()
    {
        hitbox = GetComponent<Collider>();
        DisableHitbox();
    }

    public void EnableHitbox()
    {
        lastHitTime.Clear();
        if (hitbox != null) hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitbox != null) hitbox.enabled = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit !");
        TryDamage(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit !");
        TryDamage(other.gameObject);
    }

    void TryDamage(GameObject target)
    {
        if (((1 << target.layer) & enemyLayer) == 0) return;

        if (lastHitTime.TryGetValue(target, out float lastTime) && Time.time - lastTime < hitCooldown)
            return;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            lastHitTime[target] = Time.time;
            damageable.TakeDamage(damage);
            Debug.Log("Dealt " + damage + " damage to " + target.name);
            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}
