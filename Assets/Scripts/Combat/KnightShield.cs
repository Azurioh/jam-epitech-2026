using Unity.Netcode;
using UnityEngine;

public class KnightShield : NetworkBehaviour, IAbility
{
    [Header("Shield Settings")]
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private float shieldDuration = 3f;

    private ShieldEffect shieldEffect;
    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        Debug.Log("🛡️ KnightShield: Awake called");
        shieldEffect = GetComponent<ShieldEffect>();
        if (shieldEffect == null)
        {
            Debug.LogError("🛡️ KnightShield: ShieldEffect component not found!");
        }
        else
        {
            Debug.Log("✅ KnightShield: ShieldEffect found!");
        }
    }

    public void Activate()
    {
        Debug.Log($"🛡️ KnightShield: Activate called! IsReady={IsReady}");

        if (!IsReady)
        {
            Debug.Log($"🛡️ KnightShield: Not ready! {TimeUntilReady:F1}s");
            return;
        }

        lastUseTime = Time.time;
        Debug.Log("🛡️ KnightShield: Activating shield effect...");

        if (shieldEffect != null)
        {
            shieldEffect.ActivateShieldForDuration(shieldDuration);
            Debug.Log("✅ KnightShield: Shield effect activated!");
        }
        else
        {
            Debug.LogError("🛡️ KnightShield: shieldEffect is NULL!");
        }

        if (IsOwner)
        {
            Debug.Log("🛡️ KnightShield: Sending ServerRpc...");
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
