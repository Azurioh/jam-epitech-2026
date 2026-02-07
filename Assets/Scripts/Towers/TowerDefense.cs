using Unity.Netcode;
using UnityEngine;

public class TowerDefense : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float fireRate = 1f;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab; // Glisse le Prefab Projectile ici
    [SerializeField] private Transform firePoint;         // Crée un objet vide "Muzzle" au bout du canon

    private float fireCooldown = 0f;

    void Update()
    {
        if (!IsServer) return;

        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0) return;

        GameObject target = FindClosestEnemy();

        if (target != null)
        {
            Shoot(target.transform);
            fireCooldown = fireRate;
        }
    }

    void Shoot(Transform targetTransform)
    {
        // 1. On instancie le projectile côté serveur
        // Si tu n'as pas créé de firePoint, utilise transform.position + Vector3.up
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;

        GameObject projectileGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // 2. On récupère le script pour lui donner sa cible
        HomingProjectile projectileScript = projectileGO.GetComponent<HomingProjectile>();
        if (projectileScript != null)
        {
            projectileScript.SetTarget(targetTransform);
        }

        // 3. CRUCIAL : On fait apparaître l'objet sur le réseau
        projectileGO.GetComponent<NetworkObject>().Spawn();
    }

    GameObject FindClosestEnemy()
    {
        // ... (Garde ton code actuel ici, il est très bien) ...
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float distance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float curDist = Vector3.Distance(transform.position, enemy.transform.position);
            if (curDist < distance && curDist <= range)
            {
                closest = enemy;
                distance = curDist;
            }
        }
        return closest;
    }

    // Petit bonus : Pour voir la portée dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
