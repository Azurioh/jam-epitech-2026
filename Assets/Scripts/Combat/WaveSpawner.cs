using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WaveSpawner : NetworkBehaviour
{
    [Serializable]
    public class EnemySpawnInfo
    {
        public GameObject prefab;
        [Range(0, 100)]
        public int spawnWeight = 10;
    }

    [Serializable]
    public class Wave
    {
        [Tooltip("Nombre d'ennemis dans cette wave")]
        public int enemyCount = 10;

        [Tooltip("Liste des types d'ennemis et leur poids de spawn")]
        public List<EnemySpawnInfo> enemyTypes = new List<EnemySpawnInfo>();

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
    public int totalWaves = 20;
    public int baseMobCount = 5;
    public bool infiniteMode = true;

    [Header("Enemy Prefabs")]
    public GameObject classicPrefab;
    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject rangerPrefab;
    public GameObject bigClassicPrefab;
    public GameObject bigWarriorPrefab;
    public GameObject bigMagePrefab;
    public GameObject bigRangerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 2f;

    [Header("Enemy Target")]
    [Tooltip("Cible principale des ennemis (ex: le château). Si null, cherche un objet nommé 'Castle'")]
    [SerializeField] private Transform enemyMainTarget;
    [Tooltip("Nom de l'objet à chercher comme cible si enemyMainTarget n'est pas assigné")]
    [SerializeField] private string enemyTargetName = "Castle";

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsServer) return;
        
        // Trouver la cible principale si pas assignée
        if (enemyMainTarget == null && !string.IsNullOrEmpty(enemyTargetName))
        {
            GameObject targetObj = GameObject.Find(enemyTargetName);
            if (targetObj != null)
            {
                enemyMainTarget = targetObj.transform;
                Debug.Log($"Cible principale trouvée : {enemyTargetName}");
            }
            else
            {
                Debug.LogWarning($"Impossible de trouver la cible '{enemyTargetName}'. Les ennemis n'auront pas de destination !");
            }
        }
        
        waves.Clear();
        GenerateDefaultWaves();
        
        if (autoStart)
        {
            StartWaves();
        }
    }

    private void GenerateDefaultWaves()
    {
        waves.Clear();

        int wavesPerPhase = Mathf.Max(1, totalWaves / 4);

        Debug.Log($"Génération des waves : totalWaves={totalWaves}, baseMobCount={baseMobCount}, wavesPerPhase={wavesPerPhase}");

        // Phase 1 : Classic + Warrior (baseMobCount mobs)
        for (int i = 1; i <= wavesPerPhase; i++)
        {
            Wave wave = new Wave();
            wave.enemyCount = Mathf.Max(1, baseMobCount * 1);
            wave.spawnInterval = 0.5f;
            wave.enemyTypes = new List<EnemySpawnInfo>
            {
                new EnemySpawnInfo { prefab = classicPrefab, spawnWeight = 80 },
                new EnemySpawnInfo { prefab = warriorPrefab, spawnWeight = 20 }
            };
            waves.Add(wave);
            Debug.Log($"Phase 1 - Wave {waves.Count} créée : {wave.enemyCount} ennemis");
        }

        // Phase 2 : Warrior + Mage + Ranger (baseMobCount * 2 mobs)
        for (int i = 1; i <= wavesPerPhase; i++)
        {
            Wave wave = new Wave();
            wave.enemyCount = Mathf.Max(1, baseMobCount * 2);
            wave.spawnInterval = 0.5f;
            wave.enemyTypes = new List<EnemySpawnInfo>
            {
                new EnemySpawnInfo { prefab = warriorPrefab, spawnWeight = 50 },
                new EnemySpawnInfo { prefab = magePrefab, spawnWeight = 25 },
                new EnemySpawnInfo { prefab = rangerPrefab, spawnWeight = 25 }
            };
            waves.Add(wave);
            Debug.Log($"Phase 2 - Wave {waves.Count} créée : {wave.enemyCount} ennemis");
        }

        // Phase 3 : Classic + Big Classic + Big Warrior (baseMobCount * 3 mobs)
        for (int i = 1; i <= wavesPerPhase; i++)
        {
            Wave wave = new Wave();
            wave.enemyCount = Mathf.Max(1, baseMobCount * 3);
            wave.spawnInterval = 0.5f;
            wave.enemyTypes = new List<EnemySpawnInfo>
            {
                new EnemySpawnInfo { prefab = classicPrefab, spawnWeight = 50 },
                new EnemySpawnInfo { prefab = bigClassicPrefab, spawnWeight = 25 },
                new EnemySpawnInfo { prefab = bigWarriorPrefab, spawnWeight = 25 }
            };
            waves.Add(wave);
            Debug.Log($"Phase 3 - Wave {waves.Count} créée : {wave.enemyCount} ennemis");
        }

        // Phase 4 : Warrior + Big Warrior + Big Classic (baseMobCount * 4 mobs)
        for (int i = 1; i <= wavesPerPhase; i++)
        {
            Wave wave = new Wave();
            wave.enemyCount = Mathf.Max(1, baseMobCount * 4);
            wave.spawnInterval = 0.4f;
            wave.enemyTypes = new List<EnemySpawnInfo>
            {
                new EnemySpawnInfo { prefab = warriorPrefab, spawnWeight = 40 },
                new EnemySpawnInfo { prefab = bigWarriorPrefab, spawnWeight = 30 },
                new EnemySpawnInfo { prefab = bigClassicPrefab, spawnWeight = 30 }
            };
            waves.Add(wave);
            Debug.Log($"Phase 4 - Wave {waves.Count} créée : {wave.enemyCount} ennemis");
        }

        Debug.Log($"===== TOTAL : {waves.Count} waves générées =====");
    }

    public void StartWaves()
    {
        if (!IsServer) return;
        
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

            // Attendre que tous les ennemis de la wave actuelle soient morts
            while (_enemiesAlive > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log($"Wave {_currentWaveIndex + 1} terminée ! Tous les ennemis sont morts.");

            // Délai avant la prochaine wave
            if (i < waves.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        if (infiniteMode)
        {
            Debug.Log("Mode infini activé ! Waves random avec tous les Big.");
            while (true)
            {
                _currentWaveIndex++;
                Wave randomWave = GenerateRandomBigWave();
                yield return StartCoroutine(SpawnWave(randomWave));

                // Attendre que tous les ennemis soient morts
                while (_enemiesAlive > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                Debug.Log($"Wave infinie {_currentWaveIndex + 1} terminée !");
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }
        else
        {
            _isSpawning = false;
            Debug.Log("Toutes les waves sont terminées !");
        }
    }

    private Wave GenerateRandomBigWave()
    {
        Wave wave = new Wave();
        wave.enemyCount = baseMobCount * 5;
        wave.spawnInterval = 0.3f;
        wave.enemyTypes = new List<EnemySpawnInfo>
        {
            new EnemySpawnInfo { prefab = bigClassicPrefab, spawnWeight = 25 },
            new EnemySpawnInfo { prefab = bigWarriorPrefab, spawnWeight = 25 },
            new EnemySpawnInfo { prefab = bigMagePrefab, spawnWeight = 25 },
            new EnemySpawnInfo { prefab = bigRangerPrefab, spawnWeight = 25 }
        };
        return wave;
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        Debug.Log($"Wave {_currentWaveIndex + 1} commence : {wave.enemyCount} ennemis");

        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnEnemy(wave);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private void SpawnEnemy(Wave wave)
    {
        GameObject prefab = GetRandomEnemyPrefab(wave.enemyTypes);

        if (prefab == null)
        {
            Debug.LogError("Aucun prefab d'ennemi disponible");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.Initialize(playerMask, towerMask, wallMask);
            
            // Assigner la cible principale via reflection pour accéder au champ privé
            if (enemyMainTarget != null)
            {
                var aiType = typeof(EnemyAI);
                var fallbackField = aiType.GetField("fallbackTarget", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                if (fallbackField != null)
                {
                    fallbackField.SetValue(ai, enemyMainTarget);
                }
            }
            // Sinon, assigner le nom de la cible
            else if (!string.IsNullOrEmpty(enemyTargetName))
            {
                ai.fallbackTargetName = enemyTargetName;
            }
        }

        if (wave.splitChance > 0f && UnityEngine.Random.Range(0f, 100f) < wave.splitChance)
        {
            EnemySplitter splitter = enemy.AddComponent<EnemySplitter>();
            
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

        _enemiesAlive++;
        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            StartCoroutine(WaitForDeath(health));
        }
    }

    private GameObject GetRandomEnemyPrefab(List<EnemySpawnInfo> enemyTypes)
    {
        if (enemyTypes == null || enemyTypes.Count == 0)
            return null;

        int totalWeight = 0;
        foreach (var enemy in enemyTypes)
        {
            if (enemy.prefab != null)
                totalWeight += enemy.spawnWeight;
        }

        if (totalWeight == 0)
            return null;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var enemy in enemyTypes)
        {
            if (enemy.prefab == null)
                continue;

            currentWeight += enemy.spawnWeight;
            if (randomValue < currentWeight)
            {
                return enemy.prefab;
            }
        }

        return enemyTypes[0].prefab;
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
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return spawnPoint.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        else
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
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
