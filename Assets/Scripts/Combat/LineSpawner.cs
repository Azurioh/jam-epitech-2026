using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LineSpawner : NetworkBehaviour, IAbility
{
    [Header("Line Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float cooldown = 12f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private int spawnCount = 10;
    [SerializeField] private float spacing = 1f;
    [SerializeField] private float delayBetweenSpawns = 0.1f;
    [SerializeField] private float startOffset = 1.5f;

    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    public void Activate()
    {
        if (!IsReady) return;

        lastUseTime = Time.time;

        Vector3 forward = transform.forward;
        Vector3 startPos = transform.position + forward * startOffset;

        if (IsOwner)
        {
            SpawnLineServerRpc(startPos, forward);
        }
    }

    [ServerRpc]
    private void SpawnLineServerRpc(Vector3 startPos, Vector3 direction)
    {
        StartCoroutine(SpawnLineCoroutine(startPos, direction));
    }

    private IEnumerator SpawnLineCoroutine(Vector3 startPos, Vector3 direction)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = startPos + direction * (spacing * i);
            GameObject obj = Instantiate(prefabToSpawn, spawnPos, Quaternion.LookRotation(direction));
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }
}
