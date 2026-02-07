using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum TargetPriority
    {
        Closest,
        PreferTowers,
        PreferPlayers
    }

    [Header("Behavior")]
    [SerializeField] private TargetPriority targetPriority = TargetPriority.PreferTowers;
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private DamageOnContact weaponHitbox;

    [Header("Layers")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask towerMask;
    [SerializeField] private LayerMask wallMask;

    [Header("Fallback")]
    [Tooltip("Cible manuelle si aucune cible n'est trouvée (optionnel)")]
    [SerializeField] private Transform fallbackTarget;

    [Tooltip("Layer pour chercher automatiquement une fallback target si fallbackTarget n'est pas assigné")]
    [SerializeField] private LayerMask fallbackTargetMask;

    [Header("References")]
    [SerializeField] private Transform eye;

    private NavMeshAgent _agent;
    public Animator _animator;
    private Transform _mainTarget;
    private Transform _currentTarget;
    private float _nextAttackTime;
    private float _lastAttackTime = -999f;
    private float _nextRetargetTime;
    private float _baseAgentSpeed;

    private Health _health;
    private LagEffectReceiver _lagReceiver;

    private bool _isDead;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int deathHash = Animator.StringToHash("Death");

    private int TargetMask => playerMask | towerMask;
    private int ObstacleMask => playerMask | towerMask | wallMask;

    public void Initialize(LayerMask playerLayer, LayerMask towerLayer, LayerMask wallLayer)
    {
        playerMask = playerLayer;
        towerMask = towerLayer;
        wallMask = wallLayer;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();
        _lagReceiver = GetComponent<LagEffectReceiver>();
        if (_agent == null)
        {
            Debug.LogError("EnemyAI requires a NavMeshAgent.", this);
        }
        else
        {
            _baseAgentSpeed = _agent.speed;
        }

        if (fallbackTarget == null && fallbackTargetMask != 0)
        {
            fallbackTarget = FindFallbackTargetFromLayer();
        }

        if (_health != null)
        {
            _health._currentHealth.OnValueChanged += OnHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health._currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (_isDead || _animator == null) return;

        if (newValue <= 0f)
        {
            _isDead = true;
            _animator.SetTrigger(deathHash);
            if (_agent != null) _agent.isStopped = true;
            enabled = false;
        }
        else if (newValue < oldValue)
        {
            _animator.SetTrigger(hitHash);
        }
    }

    private Transform FindFallbackTargetFromLayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1000f, fallbackTargetMask);
        if (hits.Length > 0)
        {
            return hits[0].transform;
        }
        return null;
    }

    private void Update()
    {
        bool isLagged = _lagReceiver != null && _lagReceiver.IsLagged;
        float lagMultiplier = isLagged ? _lagReceiver.GetSpeedMultiplier() : 1f;

        if (_animator != null)
        {
            if (isLagged)
            {
                _animator.speed = 0f;
                if (_lagReceiver.TryGetAnimationDelta(out float animDelta))
                {
                    _animator.Update(animDelta);
                }
            }
            else
            {
                _animator.speed = 1f;
            }
        }

        if (_agent != null)
        {
            _agent.speed = _baseAgentSpeed * lagMultiplier;
        }

        if (Time.time >= _nextRetargetTime)
        {
            _mainTarget = AcquireMainTarget();
            _nextRetargetTime = Time.time + 0.35f;
        }

        _currentTarget = ResolveBlockingTarget(_mainTarget) ?? _mainTarget ?? FindAttackableInRange();

        if (_currentTarget == null || _agent == null)
        {
            if (_agent != null && fallbackTarget != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(fallbackTarget.position);
            }
            else if (_agent != null)
            {
                _agent.isStopped = true;
            }
        }
        else
        {
            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            bool inAttackRange = distance <= attackRange;

            _agent.isStopped = inAttackRange;
            if (!inAttackRange)
            {
                _agent.SetDestination(_currentTarget.position);
            }

            if (inAttackRange)
            {
                TryAttack(_currentTarget);
            }
        }

        // Désactiver la hitbox après le cooldown
        if (weaponHitbox != null && Time.time - _lastAttackTime >= attackCooldown)
        {
            weaponHitbox.DisableHitbox();
        }

        if (_animator != null && _agent != null)
        {
            float speed = _agent.velocity.magnitude / _agent.speed;
            _animator.SetFloat(speedHash, speed);
        }
    }

    private Transform AcquireMainTarget()
    {
        switch (targetPriority)
        {
            case TargetPriority.PreferPlayers:
                return GetNearestTarget(playerMask) ?? GetNearestTarget(towerMask);
            case TargetPriority.PreferTowers:
                return GetNearestTarget(towerMask) ?? GetNearestTarget(playerMask);
            default:
                return GetNearestTarget(TargetMask);
        }
    }

    private Transform GetNearestTarget(int mask)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, mask, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform candidate = hits[i].transform;
            float dist = Vector3.Distance(transform.position, candidate.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    private Transform ResolveBlockingTarget(Transform mainTarget)
    {
        if (mainTarget == null)
        {
            return null;
        }

        Vector3 origin = eye != null ? eye.position : transform.position + Vector3.up * 0.6f;
        Vector3 direction = mainTarget.position - origin;
        float distance = direction.magnitude;

        if (distance <= 0.1f)
        {
            return null;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, ObstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == mainTarget)
            {
                return null;
            }

            if (TryGetDamageable(hit.transform, out _, out Transform root))
            {
                return root;
            }
        }

        return null;
    }

    private Transform FindAttackableInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, ObstacleMask, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!TryGetDamageable(hits[i].transform, out _, out Transform root))
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, root.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = root;
            }
        }

        return best;
    }

    private void TryAttack(Transform target)
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        _nextAttackTime = Time.time + attackCooldown;
        _lastAttackTime = Time.time;
        if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        if (_animator != null) _animator.SetTrigger(attackHash);
    }

    private bool TryGetDamageable(Transform target, out IDamageable damageable, out Transform root)
    {
        damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            root = ((Component)damageable).transform;
            return true;
        }

        root = null;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
