using Unity.Netcode;
using UnityEngine;

public class RangerArrowRain : NetworkBehaviour, IAbility
{
    [Header("Arrow Rain Settings")]
    [SerializeField] private float cooldown = 12f;
    [SerializeField] private float attackLockDuration = 1.0f;
    [SerializeField] private GameObject rainArrowPrefab;
    [SerializeField] private int arrowCount = 20;
    [SerializeField] private float rainRadius = 5f;
    [SerializeField] private float rainDuration = 3f;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float damage = 30f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastUseTime = -999f;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        if (rainArrowPrefab == null)
        {
            rainArrowPrefab = Resources.Load<GameObject>("Assets 3D/Character/Attacks/Ranger/RainArrow");
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        StartCoroutine(SpawnArrowRain());

        if (IsOwner)
        {
            ActivateArrowRainServerRpc();
        }
    }

    System.Collections.IEnumerator SpawnArrowRain()
    {
        if (rainArrowPrefab == null)
        {
            yield break;
        }

        Vector3 targetPosition = transform.position + transform.forward * 5f;
        float spawnInterval = rainDuration / arrowCount;

        for (int i = 0; i < arrowCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * rainRadius;
            Vector3 spawnPosition = targetPosition + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

            GameObject arrow = Instantiate(rainArrowPrefab, spawnPosition, Quaternion.Euler(90f, 0f, 0f));

            arrow.SetActive(true);
            foreach (Transform child in arrow.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
            }

            ParticleSystem[] particleSystems = arrow.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem arrowPs in particleSystems)
            {
                arrowPs.Play();
            }

            var arrowScript = arrow.AddComponent<FallingArrow>();
            arrowScript.damage = damage;
            arrowScript.enemyLayer = enemyLayer;

            Destroy(arrow, 5f);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    [ServerRpc]
    private void ActivateArrowRainServerRpc()
    {
        ActivateArrowRainClientRpc();
    }

    [ClientRpc]
    private void ActivateArrowRainClientRpc()
    {
        if (!IsOwner)
        {
            StartCoroutine(SpawnArrowRain());
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 targetPosition = transform.position + transform.forward * 5f;
        Gizmos.DrawSphere(targetPosition, rainRadius);
    }
}

public class FallingArrow : MonoBehaviour
{
    public float damage = 30f;
    public LayerMask enemyLayer;
    private float fallSpeed = 15f;
    private bool hasHit = false;

    void Update()
    {
        if (!hasHit)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            if (transform.position.y <= 0.5f)
            {
                hasHit = true;
                CheckForEnemies();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            hasHit = true;

            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, true);
            }
        }
    }

    void CheckForEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1f, enemyLayer);
        foreach (Collider col in hits)
        {
            Health health = col.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, true);
            }
        }
    }
}
