using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    private float _currentHealth;
    private bool _preventSplit;

    public float CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0f;
    public bool PreventSplit => _preventSplit;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, false);
    }

    public void TakeDamage(float amount, bool preventSplit)
    {
        if (!IsAlive)
        {
            return;
        }

        if (!preventSplit && amount >= maxHealth)
        {
            preventSplit = true;
        }

        if (preventSplit)
        {
            _preventSplit = true;
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
            StartCoroutine(DestroyAfterDelay(0.05f));
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
