using Unity.Netcode;
using UnityEngine;

public class KnightHulk : NetworkBehaviour, IAbility
{
    [Header("Hulk Settings")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private float hulkDuration = 5f;

    private HulkEffect hulkEffect;
    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        hulkEffect = GetComponent<HulkEffect>();
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
        }
    }
}
