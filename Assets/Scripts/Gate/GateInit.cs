using Unity.Netcode;
using UnityEngine;

public class GateInit : NetworkBehaviour, IDamageable
{
    [SerializeField] private int startHealth = 1000;
    [SerializeField] private int startMaxHealth = 1000;

    public NetworkVariable<int> health = new NetworkVariable<int>(1000);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(1000);

    public enum TypeSelection { North, South, West, East }
    public TypeSelection direction;
    public GameObject gate;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = startHealth;
            maxHealth.Value = startMaxHealth;
        }

        health.OnValueChanged += OnHealthChanged;
        UpdateGate(health.Value);
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        UpdateGate(newValue);
    }

    private void UpdateGate(int currentHealth)
    {
        if (!gate) return;
        gate.SetActive(currentHealth > 0);
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        health.Value = Mathf.Max(0, health.Value - (int)damage);
    }
}
