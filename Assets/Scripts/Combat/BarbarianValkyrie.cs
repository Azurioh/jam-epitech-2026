using Unity.Netcode;
using UnityEngine;

public class BarbarianValkyrie : NetworkBehaviour, IAbility
{
    [Header("Valkyrie Settings")]
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float attackLockDuration = 1.5f;
    [SerializeField] private float aoeRadius = 5f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float valkyrieDuration = 5f;
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastUseTime = -999f;
    private bool isValkyrieActive = false;
    private DamageOnContact damageOnContact;
    private float normalDamage;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        damageOnContact = GetComponentInChildren<DamageOnContact>();

        if (damageOnContact != null)
        {
            var damageField = typeof(DamageOnContact).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                normalDamage = (float)damageField.GetValue(damageOnContact);
            }
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        OnValkyrieHit();

        if (IsOwner)
        {
            ActivateValkyrieServerRpc();
        }
    }

    public void OnValkyrieHit()
    {
        DealAOEDamage();

        if (!isValkyrieActive)
        {
            StartCoroutine(ValkyrieMode());
        }
    }

    System.Collections.IEnumerator ValkyrieMode()
    {
        isValkyrieActive = true;

        if (damageOnContact != null)
        {
            var damageField = typeof(DamageOnContact).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                float boostedDamage = normalDamage * damageMultiplier;
                damageField.SetValue(damageOnContact, boostedDamage);
            }
        }

        yield return new WaitForSeconds(valkyrieDuration);

        if (damageOnContact != null)
        {
            var damageField = typeof(DamageOnContact).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(damageOnContact, normalDamage);
            }
        }

        isValkyrieActive = false;
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
        if (!IsOwner)
        {
            OnValkyrieHit();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, aoeRadius);
    }
}
