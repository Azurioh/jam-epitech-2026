using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public enum EnemyType
    {
        Melee,
        Ranged
    }

    [Serializable]
    public class Wave
    {
        [Tooltip("Nombre d'ennemis dans cette wave")]
        public int enemyCount = 5;

        [Tooltip("Type d'ennemis (Melee ou Ranged)")]
        public EnemyType enemyType = EnemyType.Melee;

        [Tooltip("Délai en secondes entre chaque spawn d'ennemi")]
        public float spawnInterval = 0.5f;

        [Header("Split Settings")]
        [Tooltip("Pourcentage d'ennemis qui se dédoublent à la mort (0-100)")]
        [Range(0f, 100f)]
        public float splitChance = 0f;

        [Tooltip("Nombre de copies créées quand un ennemi split")]
        [Range(2, 4)]
        public int splitCount = 2;

        [Tooltip("Les copies peuvent-elles aussi split ?")]
        public bool copiesCanSplit = false;
    }

    [Header("Wave Configuration")]
    [SerializeField] private List<Wave> waves = new List<Wave>();
    [SerializeField] private float delayBetweenWaves = 5f;
    [SerializeField] private bool autoStart = true;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private GameObject rangedPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 2f;

    [Header("Layers")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask towerMask;
    [SerializeField] private LayerMask wallMask;

    private int _currentWaveIndex = -1;
    private int _enemiesAlive;
    private bool _isSpawning;

    public int CurrentWaveIndex => _currentWaveIndex;
    public int TotalWaves => waves.Count;
    public bool IsComplete => _currentWaveIndex >= waves.Count - 1 && _enemiesAlive == 0;

    private void Start()
    {
        if (autoStart)
        {
            StartWaves();
        }
    }

    public void StartWaves()
    {
        if (!_isSpawning && _currentWaveIndex < waves.Count - 1)
        {
            StartCoroutine(SpawnWavesRoutine());
        }
    }

    private IEnumerator SpawnWavesRoutine()
    {
        _isSpawning = true;

        for (int i = _currentWaveIndex + 1; i < waves.Count; i++)
        {
            _currentWaveIndex = i;
            yield return StartCoroutine(SpawnWave(waves[i]));

            // // Attendre que tous les ennemis soient morts avant la prochaine wave
            // while (_enemiesAlive > 0)
            // {
            //     yield return new WaitForSeconds(0.5f);
            // }

            // Délai entre les waves
            if (i < waves.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        _isSpawning = false;
        Debug.Log("Toutes les waves sont terminées !");
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        Debug.Log($"Wave {_currentWaveIndex + 1} commence : {wave.enemyCount} ennemis de type {wave.enemyType}");

        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnEnemy(wave);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private void SpawnEnemy(Wave wave)
    {
        GameObject prefab = wave.enemyType == EnemyType.Melee ? meleePrefab : rangedPrefab;

        if (prefab == null)
        {
            Debug.LogError($"Pas de prefab assigné pour le type {wave.enemyType}");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Configure l'EnemyAI
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.Initialize(playerMask, towerMask, wallMask);
        }

        // Ajoute le splitter si besoin (selon la chance configurée)
        if (wave.splitChance > 0f && UnityEngine.Random.Range(0f, 100f) < wave.splitChance)
        {
            EnemySplitter splitter = enemy.AddComponent<EnemySplitter>();
            
            // Configure le splitter via reflection
            var splitterType = typeof(EnemySplitter);
            
            var splitCountField = splitterType.GetField("splitCount", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (splitCountField != null)
            {
                splitCountField.SetValue(splitter, wave.splitCount);
            }
            
            var copiesCanSplitField = splitterType.GetField("copiesCanSplit", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (copiesCanSplitField != null)
            {
                copiesCanSplitField.SetValue(splitter, wave.copiesCanSplit);
            }
        }

        // Track l'ennemi
        _enemiesAlive++;
        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            // Subscribe à la mort
            StartCoroutine(WaitForDeath(health));
        }
    }

    private IEnumerator WaitForDeath(Health health)
    {
        while (health != null && health.IsAlive)
        {
            yield return new WaitForSeconds(0.5f);
        }
        _enemiesAlive--;
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Choisit un spawn point aléatoire
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            
            // Ajoute un offset aléatoire dans le rayon
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return spawnPoint.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        else
        {
            // Spawn autour du spawner
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dessine les spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, spawnRadius);
                }
            }
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
