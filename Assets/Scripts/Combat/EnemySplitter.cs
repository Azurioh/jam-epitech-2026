using UnityEngine;

public class EnemySplitter : MonoBehaviour
{
    [Header("Split Settings")]
    [Tooltip("Nombre de copies créées à la mort")]
    [SerializeField] private int splitCount = 2;
    
    [Tooltip("Les copies peuvent-elles aussi se split ?")]
    [SerializeField] private bool copiesCanSplit = false;
    
    [Tooltip("Rayon autour de l'ennemi où les copies apparaissent")]
    [SerializeField] private float splitRadius = 1.5f;

    private Health _health;
    private bool _hasSplit;

    private void Awake()
    {
        _health = GetComponent<Health>();
        if (_health == null)
        {
            Debug.LogError("EnemySplitter requires a Health component!", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_hasSplit && _health != null && !_health.IsAlive)
        {
            if (_health.PreventSplit)
            {
                _hasSplit = true;
                return;
            }

            _hasSplit = true;
            SplitOnDeath();
        }
    }

    private void SplitOnDeath()
    {
        for (int i = 0; i < splitCount; i++)
        {
            SpawnCopy(i);
        }
    }

    private void SpawnCopy(int index)
    {
        Vector2 randomCircle = Random.insideUnitCircle * splitRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject copy = Instantiate(gameObject, spawnPos, transform.rotation);
        
        copy.transform.localScale = transform.localScale;

        Health copyHealth = copy.GetComponent<Health>();
        if (copyHealth != null)
        {
            var healthType = typeof(Health);
            var maxHealthField = healthType.GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null)
            {
                float maxHealth = (float)maxHealthField.GetValue(copyHealth);
                
                var currentHealthField = healthType.GetField("_currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (currentHealthField != null)
                {
                    currentHealthField.SetValue(copyHealth, maxHealth);
                }
                
                var preventSplitField = healthType.GetField("_preventSplit", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (preventSplitField != null)
                {
                    preventSplitField.SetValue(copyHealth, false);
                }
            }
        }

        if (!copiesCanSplit)
        {
            EnemySplitter copySplitter = copy.GetComponent<EnemySplitter>();
            if (copySplitter != null)
            {
                Destroy(copySplitter);
            }
        }
        else
        {
            EnemySplitter copySplitter = copy.GetComponent<EnemySplitter>();
            if (copySplitter != null)
            {
                var splitterType = typeof(EnemySplitter);
                var hasSplitField = splitterType.GetField("_hasSplit", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (hasSplitField != null)
                {
                    hasSplitField.SetValue(copySplitter, false);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, splitRadius);
    }
}
