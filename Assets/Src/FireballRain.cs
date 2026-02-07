using UnityEngine;
using UnityEngine.InputSystem;

public class FireballRain : MonoBehaviour
{
    public GameObject fireballPrefab;
    public float spawnHeight = 20f;
    public float spawnRate;
    public float rainDuration;
    public bool autoStart = true;

    private float nextSpawnTime;
    private float rainEndTime;
    private bool isRaining;
    private float spawnRadius = 0f;

    void Start()
    {
        if (autoStart)
        {
            StartRain();
        }
    }

    void Update()
    {
        if (isRaining && Time.time >= nextSpawnTime)
        {

            if (rainDuration > 0 && Time.time >= rainEndTime)
            {
                StopRain();
                return;
            }

            SpawnFireball();
            nextSpawnTime = Time.time + spawnRate;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartRain();
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            StopRain();
        }
    }

    void SpawnFireball()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        Quaternion randomRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), 0f);

        Debug.Log($"Spawn à: {spawnPosition} | Distance du centre: {randomCircle.magnitude} | Radius: {spawnRadius}");
        Instantiate(fireballPrefab, spawnPosition, randomRotation);
    }

    public void StartRain()
    {
        isRaining = true;
        nextSpawnTime = Time.time;
        if (rainDuration > 0)
        {
            rainEndTime = Time.time + rainDuration;
        }
        Debug.Log("Pluie de feu démarrée !");
    }

    public void StopRain()
    {
        isRaining = false;
        Debug.Log("Pluie de feu arrêtée !");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, spawnRadius);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
