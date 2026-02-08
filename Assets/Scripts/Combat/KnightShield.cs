using Unity.Netcode;
using UnityEngine;

public class KnightShield : NetworkBehaviour, IAbility
{
    [Header("Shield Settings")]
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private float shieldDuration = 6f;

    private ShieldEffect shieldEffect;
    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        shieldEffect = GetComponent<ShieldEffect>();
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        if (shieldEffect != null)
        {
            shieldEffect.ActivateShieldForDuration(shieldDuration);
        }

        if (IsOwner)
        {
            ActivateShieldServerRpc();
        }
    }

    [ServerRpc]
    private void ActivateShieldServerRpc()
    {
        ActivateShieldClientRpc();
    }

    [ClientRpc]
    private void ActivateShieldClientRpc()
    {
        if (!IsOwner && shieldEffect != null)
        {
            shieldEffect.ActivateShieldForDuration(shieldDuration);
        }
    }
}
