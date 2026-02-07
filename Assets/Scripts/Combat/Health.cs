using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour, IDamageable
{
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    
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

        if (!preventSplit && amount >= _currentHealth.Value)
        {
            preventSplit = true;
        }

        if (preventSplit)
        {
            _preventSplit.Value = true;
        }

        _currentHealth.Value = Mathf.Max(0f, _currentHealth.Value - amount);
        Debug.Log("Current health: " + _currentHealth.Value);
        
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
            StartCoroutine(DestroyAfterDelay(0.05f));
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
}
