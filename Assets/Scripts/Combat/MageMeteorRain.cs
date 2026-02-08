using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class MageMeteorRain : NetworkBehaviour, IAbility
{
    [Header("Meteor Rain Settings")]
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float attackLockDuration = 1f;
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private int meteorCount = 50;
    [SerializeField] private float rainDuration = 3f;
    [SerializeField] private float rainRadius = 20f;
    [SerializeField] private float spawnHeight = 15f;
    [SerializeField] private float meteorSpeed = 20f;
    [SerializeField] private float forwardOffset = 5f;

    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        if (meteorPrefab == null)
        {
            meteorPrefab = Resources.Load<GameObject>("Fireball");
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        StartCoroutine(SpawnMeteorRain());

        if (IsOwner)
        {
            ActivateMeteorRainServerRpc();
        }
    }

    System.Collections.IEnumerator SpawnMeteorRain()
    {
        if (meteorPrefab == null)
        {
            yield break;
        }

        Vector3 targetCenter = transform.position + transform.forward * forwardOffset;
        float spawnInterval = rainDuration / meteorCount;

        for (int i = 0; i < meteorCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * rainRadius;
            Vector3 targetPos = targetCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;

            Quaternion rotation = Quaternion.Euler(90f + Random.Range(-10f, 10f), Random.Range(0f, 360f), 0f);

            GameObject meteor = Instantiate(meteorPrefab, spawnPos, rotation);

            meteor.SetActive(true);
            foreach (Transform child in meteor.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
            }

            ParticleSystem[] particleSystems = meteor.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Play();
            }

            Rigidbody rb = meteor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.down * meteorSpeed;
                rb.useGravity = true;
            }

            Destroy(meteor, 5f);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    [ServerRpc]
    private void ActivateMeteorRainServerRpc()
    {
        ActivateMeteorRainClientRpc();
    }

    [ClientRpc]
    private void ActivateMeteorRainClientRpc()
    {
        if (!IsOwner)
        {
            StartCoroutine(SpawnMeteorRain());
        }
    }
}
