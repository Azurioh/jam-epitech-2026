using Unity.Netcode;
using UnityEngine;

public class RogueSmoke : NetworkBehaviour, IAbility
{
    [Header("Smoke Settings")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private float smokeDuration = 6f;

    [Header("Slow Zone")]
    [SerializeField] private float slowZoneRadius = 4f;
    [SerializeField] private float slowMultiplier = 0.4f;
    [SerializeField] private LayerMask enemyLayer;

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

        // Spawn slow zone
        SpawnSlowZone(transform.position);
    }

    private void SpawnSlowZone(Vector3 position)
    {
        GameObject zone = new GameObject("SlowZone");
        zone.transform.position = position;

        // Collider pour la détection
        SphereCollider col = zone.AddComponent<SphereCollider>();
        col.radius = slowZoneRadius;
        col.isTrigger = true;

        // Logique de slow
        SlowZone slow = zone.AddComponent<SlowZone>();
        slow.SetParameters(slowMultiplier, enemyLayer);

        // Visuel AOE
        AOEZone aoe = zone.AddComponent<AOEZone>();
        aoe.radius = slowZoneRadius;
        aoe.duration = smokeDuration;
        aoe.zoneColor = new Color(0.5f, 0.2f, 0.8f, 0.6f);
        aoe.edgeColor = new Color(0.7f, 0.3f, 1f, 0.8f);
        aoe.affectedLayers = 0; // Pas de lag, juste le visuel
        aoe.destroyOnEnd = true;
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
