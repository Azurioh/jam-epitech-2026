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
    [Header("Knockback")]
    [SerializeField] private bool knockbackOnImpact = false;
    [SerializeField] private float knockbackRadius = 6f;
    [SerializeField] private float knockbackForce = 50f;
    [SerializeField] private float knockbackUpwardsModifier = 1.5f;
    [SerializeField] private float knockbackVelocityBoost = 20f;
    [SerializeField] private float knockbackUpwardsVelocity = 6f;
    [SerializeField] private float knockbackDuration = 0.35f;
    [SerializeField] private LayerMask knockbackMask = ~0;
    [Header("Shockwave")]
    [SerializeField] private bool shockwaveOnImpact = false;
    [SerializeField] private float shockwaveMaxRadius = 6f;
    [SerializeField] private float shockwaveDuration = 0.5f;
    [SerializeField] private float shockwaveStartWidth = 0.25f;
    [SerializeField] private float shockwaveEndWidth = 0.05f;
    [SerializeField] private Color shockwaveColor = new Color(0.6f, 0.9f, 1f, 0.9f);
    [SerializeField] private int shockwaveSegments = 48;
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

            Vector3 impactPosition = other.ClosestPoint(transform.position);
            Vector3 groundedPosition = GetGroundedPosition(impactPosition);
            SpawnAoeZone(groundedPosition);
            ApplyKnockback(impactPosition);
            SpawnShockwave(groundedPosition);

            // On détruit le projectile proprement sur le réseau
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void ApplyKnockback(Vector3 impactPosition)
    {
        if (!knockbackOnImpact) return;

        Collider[] hits = Physics.OverlapSphere(impactPosition, knockbackRadius, knockbackMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return;

        System.Collections.Generic.HashSet<Rigidbody> processed = new System.Collections.Generic.HashSet<Rigidbody>();
        System.Collections.Generic.HashSet<EnemyAI> processedEnemies = new System.Collections.Generic.HashSet<EnemyAI>();
        System.Collections.Generic.HashSet<PlayerController> processedPlayers = new System.Collections.Generic.HashSet<PlayerController>();

        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody != null ? hit.attachedRigidbody : hit.GetComponentInParent<Rigidbody>();
            if (rb == null || processed.Contains(rb) || rb.isKinematic) continue;

            processed.Add(rb);

            float distance = Vector3.Distance(impactPosition, rb.worldCenterOfMass);
            float falloff = Mathf.Clamp01(1f - (distance / knockbackRadius));
            if (falloff <= 0f) continue;

            if (knockbackVelocityBoost > 0f || knockbackUpwardsVelocity > 0f)
            {
                    Vector3 direction = rb.worldCenterOfMass - impactPosition;
                    direction.y = 0f;
                    direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
                rb.linearVelocity += direction * (knockbackVelocityBoost * falloff)
                    + Vector3.up * (knockbackUpwardsVelocity * falloff);
            }

            rb.AddExplosionForce(
                knockbackForce * falloff,
                impactPosition,
                knockbackRadius,
                knockbackUpwardsModifier,
                ForceMode.VelocityChange
            );
        }

        foreach (Collider hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || processedEnemies.Contains(enemy)) continue;

            processedEnemies.Add(enemy);

            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            if (enemyRb != null && !enemyRb.isKinematic) continue;

            float distance = Vector3.Distance(impactPosition, enemy.transform.position);
            float falloff = Mathf.Clamp01(1f - (distance / knockbackRadius));
            if (falloff <= 0f) continue;

            Vector3 direction = enemy.transform.position - impactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.zero;
            }
            else
            {
                direction.Normalize();
            }

            Vector3 impulse = direction * (knockbackVelocityBoost * falloff)
                + Vector3.up * (knockbackUpwardsVelocity * falloff);
            enemy.ApplyKnockback(impulse, knockbackDuration * falloff);
        }

        foreach (Collider hit in hits)
        {
            PlayerController playerController = hit.GetComponentInParent<PlayerController>();
            if (playerController == null || processedPlayers.Contains(playerController)) continue;

            processedPlayers.Add(playerController);

            float distance = Vector3.Distance(impactPosition, playerController.transform.position);
            float falloff = Mathf.Clamp01(1f - (distance / knockbackRadius));
            if (falloff <= 0f) continue;

            Vector3 direction = playerController.transform.position - impactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.zero;
            }
            else
            {
                direction.Normalize();
            }

            Vector3 impulse = direction * (knockbackVelocityBoost * falloff)
                + Vector3.up * (knockbackUpwardsVelocity * falloff);
            playerController.ApplyKnockbackServer(impulse, knockbackDuration * falloff);
        }
    }

    private void SpawnShockwave(Vector3 position)
    {
        if (!knockbackOnImpact || !shockwaveOnImpact) return;

        SpawnShockwaveClientRpc(position);
    }

    [ClientRpc]
    private void SpawnShockwaveClientRpc(Vector3 position)
    {
        if (!shockwaveOnImpact) return;

        GameObject shockwave = new GameObject("Shockwave");
        shockwave.transform.position = position + Vector3.up * 0.05f;

        ShockwaveEffect effect = shockwave.AddComponent<ShockwaveEffect>();
        effect.Initialize(
            shockwaveMaxRadius,
            shockwaveDuration,
            shockwaveStartWidth,
            shockwaveEndWidth,
            shockwaveColor,
            shockwaveSegments
        );
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
