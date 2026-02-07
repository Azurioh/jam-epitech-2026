using Unity.Netcode;
using UnityEngine;

public class BarbarianValkyrie : NetworkBehaviour, IAbility
{
    [Header("Valkyrie Settings")]
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float attackLockDuration = 1.5f;
    [SerializeField] private float aoeRadius = 5f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    public void Activate()
    {

        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;


        if (IsOwner)
        {
            ActivateValkyrieServerRpc();
        }
    }

    public void OnValkyrieHit()
    {
        DealAOEDamage();
    }

    void DealAOEDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, enemyLayer);

        foreach (Collider col in hits)
        {
            Health health = col.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, true);
            }
        }
    }

    [ServerRpc]
    private void ActivateValkyrieServerRpc()
    {
        ActivateValkyrieClientRpc();
    }

    [ClientRpc]
    private void ActivateValkyrieClientRpc()
    {
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, aoeRadius);
    }
}
