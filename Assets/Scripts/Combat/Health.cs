using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour, IDamageable
{
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 2f;

    [Header("Damage Numbers")]
    [SerializeField] private float damageNumberHeight = 2f;
    [SerializeField] private float criticalThreshold = 50f;

    public NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _preventSplit = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float CurrentHealth => _currentHealth.Value;
    public bool IsAlive => _currentHealth.Value > 0f;
    public bool PreventSplit => _preventSplit.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _currentHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, false);
    }

    public void TakeDamage(float amount, bool preventSplit)
    {
        if (!IsServer) return;

        if (!IsAlive)
        {
            return;
        }

        ShieldEffect shield = GetComponent<ShieldEffect>();
        if (shield != null && shield.IsShieldActive())
        {
            SpawnDamageNumberClientRpc(0f, false, false);
            return;
        }
        if (!preventSplit && amount >= maxHealth)
        {
            preventSplit = true;
        }
        if (preventSplit)
        {
            _preventSplit.Value = true;
        }

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);

        bool isCritical = amount >= criticalThreshold;
        SpawnDamageNumberClientRpc(amount, isCritical, false);

        if (!IsAlive)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (!IsServer) return;

        if (destroyOnDeath)
        {
            StartCoroutine(DestroyAfterDelay(deathDelay));
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
        Destroy(gameObject);
    }

    [ClientRpc]
    private void SpawnDamageNumberClientRpc(float damage, bool isCritical, bool isHeal)
    {
        Vector3 spawnPos = transform.position + Vector3.up * damageNumberHeight;

        GameObject numberObj = new GameObject("DamageNumber");
        numberObj.transform.position = spawnPos;

        DamageNumber damageNumber = numberObj.AddComponent<DamageNumber>();
        damageNumber.Initialize(damage, isCritical, isHeal);
    }

    public void Heal(float amount)
    {
        if (!IsServer) return;

        _currentHealth.Value = Mathf.Min(maxHealth, _currentHealth.Value + amount);
        SpawnDamageNumberClientRpc(amount, false, true);
    }
}
