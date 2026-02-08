using Unity.Netcode;
using UnityEngine;

public class CastleInit : NetworkBehaviour, IDamageable
{
    [SerializeField] private int startHealth = 5000;
    [SerializeField] private int startMaxHealth = 5000;

    public NetworkVariable<int> health = new NetworkVariable<int>(5000);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(5000);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = startHealth;
            maxHealth.Value = startMaxHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        health.Value = Mathf.Max(0, health.Value - (int)damage);
    }
}
