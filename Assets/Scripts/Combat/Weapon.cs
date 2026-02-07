using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float range = 10f;
    
    [Header("Weapon Type")]
    [Tooltip("Cette arme empêche-t-elle les ennemis de se dédoubler ?")]
    [SerializeField] private bool antiSplit = false;
    
    [Header("Target")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private Transform firePoint;
    
    private float _nextFireTime;

    public void Fire()
    {
        if (Time.time < _nextFireTime) return;
        
        _nextFireTime = Time.time + fireRate;
        
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = transform.forward;
        
        // Raycast pour détecter la cible
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, targetMask))
        {
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                // Utilise le flag antiSplit
                health.TakeDamage(damage, antiSplit);
                
                Debug.Log($"Hit {hit.collider.name} with {(antiSplit ? "ANTI-SPLIT" : "normal")} weapon");
            }
        }
    }
    
    // Alternative : attaque en zone
    public void FireAOE(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, targetMask);
        
        foreach (Collider col in hits)
        {
            Health health = col.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, antiSplit);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Gizmos.color = antiSplit ? Color.cyan : Color.red;
        Gizmos.DrawRay(origin, transform.forward * range);
    }
}
