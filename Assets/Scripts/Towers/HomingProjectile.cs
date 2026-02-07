using Unity.Netcode;
using UnityEngine;

public class HomingProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [Header("AOE")]
    [SerializeField] private bool spawnAoeZone = false;
    [SerializeField] private float aoeDuration = 10f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundOffset = 0.05f;
    private bool _isHealing = false;

    private Transform target;

    // Fonction appelée par la tourelle juste après le Spawn pour donner la cible
    public void SetTarget(Transform newTarget, bool isHealing = false)
    {
        target = newTarget;
        _isHealing = isHealing;
    }

    void Update()
    {
        // IMPORTANT : Seul le serveur calcule la trajectoire et les collisions
        if (!IsServer) return;

        // Si la cible est morte ou a disparu pendant le trajet, on détruit la balle
        if (target == null)
        {
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        // Mouvement autoguidé vers la cible
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Optionnel : Orienter le projectile vers la cible (visuel)
        transform.LookAt(target);
    }

    // Gestion de la collision
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // On vérifie qu'on a touché un ennemi
        if (other.CompareTag("Enemy"))
        {
            // TODO: Ici tu appelleras plus tard le script de vie de l'ennemi
            // ex: other.GetComponent<EnemyHealth>().TakeDamage(damage);
            other.GetComponent<Health>().TakeDamage(_isHealing ? -damage : damage);

            Vector3 spawnPosition = GetGroundedPosition(transform.position);
            SpawnAoeZone(spawnPosition);

            // On détruit le projectile proprement sur le réseau
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void SpawnAoeZone(Vector3 position)
    {
        if (!spawnAoeZone) return;

        GameObject aoeGO = new GameObject("AOEZone");
        aoeGO.transform.position = position;
        AOEZone zone = aoeGO.AddComponent<AOEZone>();

        if (zone != null)
        {
            zone.SetDuration(aoeDuration);
        }

        NetworkObject netObject = aoeGO.GetComponent<NetworkObject>();
        if (netObject != null)
        {
            netObject.Spawn();
        }
    }

    private Vector3 GetGroundedPosition(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 2f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 20f, groundMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            return position;
        }

        float bestY = float.PositiveInfinity;
        Vector3 bestPoint = position;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.normal.y < 0.3f) continue;

            if (hit.point.y < bestY)
            {
                bestY = hit.point.y;
                bestPoint = hit.point;
            }
        }

        if (bestY < float.PositiveInfinity)
        {
            return bestPoint + Vector3.up * groundOffset;
        }

        return position;
    }
}
