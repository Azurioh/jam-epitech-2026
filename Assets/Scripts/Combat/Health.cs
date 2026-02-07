using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    private float _currentHealth;

    public float CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0f;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
        {
            return;
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        if (!IsAlive)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
