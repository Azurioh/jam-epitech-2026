using UnityEngine;
using UnityEngine.InputSystem;

public class DebugKillEnemy : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private float killRadius = 10f;
    [SerializeField] private LayerMask enemyMask = ~0;

    [Header("Keys")]
    [Tooltip("Touche pour infliger 50% HP (2 coups = split)")]
    [SerializeField] private Key multiHitKey = Key.K;
    
    [Tooltip("Touche pour one-shot (1 coup = pas de split)")]
    [SerializeField] private Key oneShotKey = Key.L;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Touche K : inflige 50% des HP (2 coups pour tuer, ennemi split)
        if (Keyboard.current[multiHitKey].wasPressedThisFrame)
        {
            HitNearestEnemy(0.5f);
        }

        // Touche L : one-shot (1 coup = pas de split)
        if (Keyboard.current[oneShotKey].wasPressedThisFrame)
        {
            HitNearestEnemy(1000f);
        }
    }

    private void HitNearestEnemy(float damageAmount)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, killRadius, enemyMask);
        
        if (enemies.Length == 0)
        {
            Debug.Log("Aucun ennemi à proximité");
            return;
        }

        // Trouve l'ennemi le plus proche
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider col in enemies)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.transform;
            }
        }

        if (nearest != null)
        {
            Health health = nearest.GetComponent<Health>();
            if (health != null && health.IsAlive)
            {
                // Si damageAmount < 1, c'est un pourcentage des HP actuels
                float damage = damageAmount < 1f ? health.maxHealth * damageAmount : damageAmount;
                
                string hitType = damage >= health.maxHealth ? "ONE-SHOT (pas de split)" : $"{damage:F0} dégâts (split si mort)";
                health.TakeDamage(damage);
                Debug.Log($"[{hitType}] Hit: {nearest.name} ({health.CurrentHealth:F0} HP)");
                
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}
