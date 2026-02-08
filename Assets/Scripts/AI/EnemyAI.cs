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

    public enum AttackType
    {
        Melee,
        Ranged
    }

    [Header("Behavior")]
    [SerializeField] private TargetPriority targetPriority = TargetPriority.PreferTowers;
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.0f;

    [Header("Melee")]
    [SerializeField] private DamageOnContact weaponHitbox;

    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;

    [Header("Knockback")]
    [SerializeField] private float knockbackDamping = 10f;
    [SerializeField] private LayerMask knockbackGroundMask = ~0;
    [SerializeField] private float knockbackGroundOffset = 0.05f;

    [Header("Layers")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask towerMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask castleMask; // Nouvelle layer pour le château

    [SerializeField] private Transform fallbackTarget;
    [Header("Fallback")]
    [Tooltip("Cible manuelle si aucune cible n'est trouvée (optionnel)")]
    [SerializeField] public string fallbackTargetName;
    
    [Tooltip("Layer pour chercher automatiquement une fallback target si fallbackTarget n'est pas assigné")]
    [SerializeField] private LayerMask fallbackTargetMask;

    [Header("References")]
    [SerializeField] private Transform eye;

    private NavMeshAgent _agent;
    public Animator _animator;
    private Transform _mainTarget;
    public Transform _currentTarget;
    private float _nextAttackTime;
    private float _lastAttackTime = -999f;
    private float _nextRetargetTime;
    private Transform _cachedFallbackWall;
    private float _baseAgentSpeed;

    private Vector3 _knockbackVelocity;
    private float _knockbackTime;
    private bool _knockbackStoppedAgent;

    private Health _health;
    private LagEffectReceiver _lagReceiver;

    private bool _isDead;

    private Vector3 _lastDestination;
    private float _currentAnimSpeed;
    private const float ANIM_SPEED_SMOOTHING = 10f;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int deathHash = Animator.StringToHash("Death");

    private int TargetMask => playerMask | towerMask | wallMask | castleMask;
    private int ObstacleMask => playerMask | towerMask | wallMask | castleMask;

    /// <summary>
    /// Retourne la distance entre cet ennemi et le point le plus proche du collider de la cible.
    /// Fallback sur transform.position si pas de collider.
    /// </summary>
    private float DistanceToTarget(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 closestPoint = col.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closestPoint);
        }
        return Vector3.Distance(transform.position, target.position);
    }

    /// <summary>
    /// Retourne le point le plus proche du collider de la cible.
    /// Fallback sur transform.position si pas de collider.
    /// </summary>
    private Vector3 ClosestPointOnTarget(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            return col.ClosestPoint(transform.position);
        }
        return target.position;
    }

    /// <summary>
    /// Vérifie qu'une cible est toujours valide (non null, active, et vivante).
    /// </summary>
    private bool IsValidTarget(Transform target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        Health h = target.GetComponentInParent<Health>();
        if (h != null && !h.IsAlive) return false;

        return true;
    }

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

        if (fallbackTarget == null) {
            GameObject findObj = GameObject.Find(fallbackTargetName); 
            if (findObj != null) 
            {
                fallbackTarget = findObj.transform;
            }
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
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
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
        float deltaTime = Time.deltaTime;

        if (HandleKnockback(deltaTime))
        {
            UpdateAnimatorSpeed();
            return;
        }

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

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.speed = _baseAgentSpeed * lagMultiplier;
        }

        // Invalider les cibles mortes ou désactivées
        if (!IsValidTarget(_mainTarget))
        {
            _mainTarget = null;
        }
        if (!IsValidTarget(_currentTarget))
        {
            _currentTarget = null;
        }
        if (!IsValidTarget(_cachedFallbackWall))
        {
            _cachedFallbackWall = null;
        }

        if (Time.time >= _nextRetargetTime || _mainTarget == null)
        {
            _mainTarget = AcquireMainTarget();
            _nextRetargetTime = Time.time + 0.35f;
        }

        if (_mainTarget != null)
        {
            Transform blocking = ResolveBlockingTarget(_mainTarget);
            _currentTarget = blocking ?? _mainTarget;
        }
        else
        {
            _currentTarget = null;
        }

        if (_currentTarget == null || _agent == null || !_agent.isOnNavMesh)
        {
            if (_agent != null && _agent.isOnNavMesh && fallbackTarget != null)
            {
                // Calculer le mur une seule fois (le château/murs ne bougent jamais)
                if (_cachedFallbackWall == null)
                {
                    _cachedFallbackWall = FindWallTowardsTarget(fallbackTarget.position);
                }

                Transform targetToMoveTo = _cachedFallbackWall != null ? _cachedFallbackWall : fallbackTarget;
                float distance = DistanceToTarget(targetToMoveTo);
                bool inAttackRange = distance <= attackRange;
                
                _agent.isStopped = inAttackRange;
                if (!inAttackRange)
                {
                    Vector3 movePoint = ClosestPointOnTarget(targetToMoveTo);
                    if (Vector3.Distance(_lastDestination, movePoint) > 0.1f)
                    {
                        _agent.SetDestination(movePoint);
                        _lastDestination = movePoint;
                    }
                }
                else if (_cachedFallbackWall != null)
                {
                    TryAttack(_cachedFallbackWall);
                }
            }
            else if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }
        else
        {
            float distance = DistanceToTarget(_currentTarget);
            bool inAttackRange = distance <= attackRange;

            if (attackType == AttackType.Melee)
            {
                _agent.isStopped = inAttackRange;
                if (!inAttackRange)
                {
                    Vector3 movePoint = ClosestPointOnTarget(_currentTarget);
                    if (Vector3.Distance(_lastDestination, movePoint) > 0.5f)
                    {
                        _agent.SetDestination(movePoint);
                        _lastDestination = movePoint;
                    }
                }
            }
            else
            {
                // Le ranged s'arrête à portée pour tirer
                _agent.isStopped = inAttackRange;
                if (!inAttackRange)
                {
                    Vector3 movePoint = ClosestPointOnTarget(_currentTarget);
                    if (Vector3.Distance(_lastDestination, movePoint) > 0.5f)
                    {
                        _agent.SetDestination(movePoint);
                        _lastDestination = movePoint;
                    }
                }
            }

            if (inAttackRange)
            {
                TryAttack(_currentTarget);
            }
        }

        // Désactiver la hitbox après le cooldown (melee uniquement)
        if (attackType == AttackType.Melee && weaponHitbox != null && Time.time - _lastAttackTime >= attackCooldown)
        {
            weaponHitbox.DisableHitbox();
        }

        if (_animator != null && _agent != null && _agent.isOnNavMesh)
        {
            float targetSpeed = _agent.velocity.magnitude / _agent.speed;
            _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, targetSpeed, ANIM_SPEED_SMOOTHING * deltaTime);
            _animator.SetFloat(speedHash, _currentAnimSpeed);
        }
    }

    private void UpdateAnimatorSpeed()
    {
        if (_animator == null || _agent == null || !_agent.isOnNavMesh) return;

        float targetSpeed = _agent.velocity.magnitude / _agent.speed;
        _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, targetSpeed, ANIM_SPEED_SMOOTHING * Time.deltaTime);
        _animator.SetFloat(speedHash, _currentAnimSpeed);
    }

    private bool HandleKnockback(float deltaTime)
    {
        if (_knockbackTime <= 0f) return false;

        _knockbackTime -= deltaTime;
        float damp = Mathf.Clamp01(knockbackDamping * deltaTime);
        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, damp);

        if (_agent != null && _agent.isOnNavMesh)
        {
            if (!_knockbackStoppedAgent)
            {
                _knockbackStoppedAgent = true;
                _agent.isStopped = true;
            }
            _agent.Move(_knockbackVelocity * deltaTime);
        }
        else
        {
            transform.position += _knockbackVelocity * deltaTime;
            ClampToGround();
        }

        if (_knockbackTime <= 0f)
        {
            _knockbackVelocity = Vector3.zero;
            if (_agent != null && _agent.isOnNavMesh && _knockbackStoppedAgent)
            {
                _agent.isStopped = false;
                _knockbackStoppedAgent = false;
            }
            return false;
        }

        return true;
    }

    private void ClampToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 50f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, knockbackGroundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y >= 0.3f)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + knockbackGroundOffset, transform.position.z);
            }
        }
    }

    public void ApplyKnockback(Vector3 impulse, float duration)
    {
        _knockbackVelocity += impulse;
        _knockbackTime = Mathf.Max(_knockbackTime, duration);
    }

    private Transform AcquireMainTarget()
    {
        // Vérifier d'abord si le château est accessible (pas de mur entre nous)
        Transform castle = GetNearestTarget(castleMask);
        if (castle != null && !IsBlockedByWall(castle))
        {
            return castle;
        }

        switch (targetPriority)
        {
            case TargetPriority.PreferPlayers:
                return GetNearestTarget(playerMask) ?? GetNearestTarget(towerMask) ?? GetNearestTarget(wallMask);
            case TargetPriority.PreferTowers:
                return GetNearestTarget(towerMask) ?? GetNearestTarget(playerMask) ?? GetNearestTarget(wallMask);
            default:
                return GetNearestTarget(TargetMask);
        }
    }

    private bool IsBlockedByWall(Transform target)
    {
        Vector3 origin = eye != null ? eye.position : transform.position + Vector3.up * 0.6f;
        Vector3 direction = target.position - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, wallMask, QueryTriggerInteraction.Ignore))
        {
            // Un mur bloque le chemin vers le château
            return hit.transform != target;
        }
        return false;
    }

    private Transform GetNearestTarget(int mask)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, mask, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform candidate = hits[i].transform;

            // Ignorer les objets désactivés (gates détruits par exemple)
            if (!candidate.gameObject.activeInHierarchy) continue;

            // Ignorer les cibles mortes (0 HP)
            Health targetHealth = candidate.GetComponentInParent<Health>();
            if (targetHealth != null && !targetHealth.IsAlive) continue;

            float dist = DistanceToTarget(candidate);
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
        if (!IsValidTarget(mainTarget))
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

            float dist = DistanceToTarget(root);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = root;
            }
        }

        return best;
    }

    private Transform FindWallTowardsTarget(Vector3 targetPosition)
    {
        // Chercher les murs dans un rayon raisonnable (30 unités)
        Collider[] hits = Physics.OverlapSphere(transform.position, 30f, wallMask, QueryTriggerInteraction.Ignore);
        Transform best = null;
        float bestDist = float.MaxValue;
        
        for (int i = 0; i < hits.Length; i++)
        {
            if (!TryGetDamageable(hits[i].transform, out _, out Transform root))
            {
                continue;
            }
            float distance = DistanceToTarget(root);

            // Prendre le mur le plus proche
            if (distance < bestDist)
            {
                bestDist = distance;
                best = root;
            }
        }

        return best;
    }

    private void TryAttack(Transform target)
    {
        if (Time.time < _nextAttackTime) return;

        // Ne pas attaquer les cibles mortes ou désactivées
        if (!IsValidTarget(target)) return;

        // Regarder la cible avant d'attaquer
        Vector3 lookDir = (target.position - transform.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        _nextAttackTime = Time.time + attackCooldown;
        _lastAttackTime = Time.time;

        if (attackType == AttackType.Melee)
        {
            if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        }
        else if (attackType == AttackType.Ranged)
        {
            ShootProjectile(target);
        }

        if (_animator != null) _animator.SetTrigger(attackHash);
    }

    private void ShootProjectile(Transform target)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position + Vector3.up * 0.8f;
        Vector3 direction = (target.position + Vector3.up * 0.5f - spawnPos).normalized;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        var netObj = proj.GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj != null) netObj.Spawn();
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
