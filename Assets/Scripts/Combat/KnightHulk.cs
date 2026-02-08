using Unity.Netcode;
using UnityEngine;

public class KnightHulk : NetworkBehaviour, IAbility
{
    [Header("Hulk Settings")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private float hulkDuration = 5f;
    [SerializeField] private float damageMultiplier = 2f;

    private HulkEffect hulkEffect;
    private DamageOnContact damageOnContact;
    private float normalDamage;
    private float lastUseTime = -999f;
    private bool isHulkActive = false;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        hulkEffect = GetComponent<HulkEffect>();

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

        if (hulkEffect != null)
        {
            hulkEffect.ActivateHulkModeForDuration(hulkDuration);
        }

        if (!isHulkActive)
        {
            StartCoroutine(HulkMode());
        }

        if (IsOwner)
        {
            ActivateHulkServerRpc();
        }
    }

    [ServerRpc]
    private void ActivateHulkServerRpc()
    {
        ActivateHulkClientRpc();
    }

    [ClientRpc]
    private void ActivateHulkClientRpc()
    {
        if (!IsOwner && hulkEffect != null)
        {
            hulkEffect.ActivateHulkModeForDuration(hulkDuration);
            if (!isHulkActive)
            {
                StartCoroutine(HulkMode());
            }
        }
    }

    System.Collections.IEnumerator HulkMode()
    {
        isHulkActive = true;

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

        yield return new WaitForSeconds(hulkDuration);

        if (damageOnContact != null)
        {
            var damageField = typeof(DamageOnContact).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(damageOnContact, normalDamage);
            }
        }

        isHulkActive = false;
    }
}
