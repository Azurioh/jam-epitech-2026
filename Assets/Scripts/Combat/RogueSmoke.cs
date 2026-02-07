using Unity.Netcode;
using UnityEngine;

public class RogueSmoke : NetworkBehaviour, IAbility
{
    [Header("Smoke Settings")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private float smokeDuration = 6f;

    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        if (smokePrefab == null)
        {
            smokePrefab = Resources.Load<GameObject>("Assets 3D/Character/Attacks/Rogue/Smoke");
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        SpawnSmoke();

        if (IsOwner)
        {
            ActivateSmokeServerRpc();
        }
    }

    void SpawnSmoke()
    {
        if (smokePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
        GameObject smoke = Instantiate(smokePrefab, spawnPosition, Quaternion.identity);

        smoke.SetActive(true);

        foreach (Transform child in smoke.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.SetActive(true);
        }

        ParticleSystem[] particleSystems = smoke.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Play();
        }

        Destroy(smoke, smokeDuration);
    }

    [ServerRpc]
    private void ActivateSmokeServerRpc()
    {
        ActivateSmokeClientRpc();
    }

    [ClientRpc]
    private void ActivateSmokeClientRpc()
    {
        if (!IsOwner)
        {
            SpawnSmoke();
        }
    }
}
