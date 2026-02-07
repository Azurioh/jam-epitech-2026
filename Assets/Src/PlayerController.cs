using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    public enum AttackType
    {
        Melee,
        Ranged
    }

    [Header("Attack")]
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField] private float attackCooldown;
    [SerializeField] private DamageOnContact weaponHitbox;

    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;

    [Header("Abilities System")]
    [Tooltip("Drag & drop un script qui implémente IAbility (ex: KnightShield)")]
    [SerializeField] private MonoBehaviour abilityScript;
    [Tooltip("Drag & drop un script qui implémente IAbility (ex: KnightHulk)")]
    [SerializeField] private MonoBehaviour ultimateScript;

    private IAbility ability;
    private IAbility ultimate;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravity;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    // Components
    private CharacterController controller;
    private Animator animator;
    private PlayerInputActions inputActions;
    private Camera playerCamera;

    // Input
    private Vector2 moveInput;

    // Physics
    private Vector3 velocity;
    public bool isGrounded;

    // Smoothing
    private Vector3 currentMoveDir;
    private Vector3 moveDirVelocity;

    // Animation parameters
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int fallHash = Animator.StringToHash("IsFalling");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int special1Hash = Animator.StringToHash("Special1");
    private readonly int special2Hash = Animator.StringToHash("Special2");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int deathHash = Animator.StringToHash("Death");

    // Attack
    private float lastAttackTime = -999f;
    private float currentAttackDuration;
    private bool isAttacking;
    private bool isDead;
    private Health health;

    // --- Animation sync sur le réseau ---
    private NetworkVariable<float> networkAnimSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsFalling = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        inputActions = new PlayerInputActions();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (abilityScript != null)
        {
            ability = abilityScript as IAbility;
        }

        if (ultimateScript != null)
        {
            ultimate = ultimateScript as IAbility;
        }
    }

    void OnEnable()
    {
        PauseMenuManager.canPaused = true;
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Attack.performed += OnAttackPerformed;
        inputActions.Player.Special1.performed += OnSpecial1Performed;
        inputActions.Player.Special2.performed += OnSpecial2Performed;
    }

    void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Special1.performed -= OnSpecial1Performed;
        inputActions.Player.Special2.performed -= OnSpecial2Performed;
        inputActions.Player.Disable();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        health = GetComponent<Health>();
        if (health != null)
        {
            health._currentHealth.OnValueChanged += OnHealthChanged;
        }

        var vars = Variables.Object(this.gameObject);
        moveSpeed = vars.Get<float>("MOVE_SPEED");
        rotationSpeed = vars.Get<float>("ROTATION_SPEED");
        attackCooldown = vars.Get<float>("ATTACK_COOLDOWN");
        jumpForce = vars.Get<float>("JUMP_FORCE");
        gravity = vars.Get<float>("GRAVITY");
        weaponHitbox = vars.Get<DamageOnContact>("WEAPON_HITBOX");
        groundCheck = vars.Get<Transform>("GROUND_CHECK");

        GameObject[] spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawns.Length > 0)
        {
            int index = (int)(OwnerClientId % (ulong)spawns.Length);
            // Désactiver le CharacterController sinon transform.position est ignoré
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = spawns[index].transform.position;
            transform.rotation = spawns[index].transform.rotation;
            if (cc != null) cc.enabled = true;
        }

        playerCamera = GetComponentInChildren<Camera>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Détruire la caméra de la scène s'il y en a une (évite les conflits avec Camera.main)
        if (IsOwner)
        {
            GameObject sceneCamera = GameObject.FindWithTag("MainCamera");
            if (sceneCamera != null && !sceneCamera.transform.IsChildOf(transform))
            {
                Destroy(sceneCamera);
            }
        }

        if (IsOwner)
        {
            // Activer la caméra et l'audio pour le joueur local
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";

                var audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null) audioListener.enabled = true;

                // Activer le CinemachineBrain pour que la caméra suive le joueur local
                var brain = playerCamera.GetComponent<CinemachineBrain>();
                if (brain != null) brain.enabled = true;
            }

            // Activer le contrôle caméra Cinemachine pour le joueur local
            var cinemachine = GetComponentInChildren<CinemachineMouseController>();
            if (cinemachine != null) cinemachine.enabled = true;

            // Activer les CinemachineCamera (virtual cameras) du joueur local
            foreach (var vcam in GetComponentsInChildren<CinemachineCamera>())
            {
                vcam.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // --- Désactiver tout pour les joueurs distants (proxies) ---

            // Désactiver la caméra du proxy
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                playerCamera.tag = "Untagged"; // Empêcher Camera.main de le trouver

                var audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null) audioListener.enabled = false;

                // IMPORTANT : Désactiver le CinemachineBrain du proxy
                // Sinon il interfère avec le système global de Cinemachine
                var brain = playerCamera.GetComponent<CinemachineBrain>();
                if (brain != null) brain.enabled = false;
            }

            // Désactiver le contrôle caméra Cinemachine du proxy
            var cinemachine = GetComponentInChildren<CinemachineMouseController>();
            if (cinemachine != null) cinemachine.enabled = false;

            // IMPORTANT : Désactiver les CinemachineCamera (virtual cameras) du proxy
            // Sinon le Brain du joueur local "voit" les virtual cameras des autres joueurs
            foreach (var vcam in GetComponentsInChildren<CinemachineCamera>())
            {
                vcam.enabled = false;
            }

            // Désactiver le CharacterController sur les proxies (NetworkTransform gère la sync)
            // Mais ajouter un CapsuleCollider pour garder les collisions physiques
            if (controller != null)
            {
                // Récupérer les dimensions du CharacterController avant de le désactiver
                float ccHeight = controller.height;
                float ccRadius = controller.radius;
                Vector3 ccCenter = controller.center;

                controller.enabled = false;

                // Ajouter un CapsuleCollider de remplacement avec les mêmes dimensions
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = ccHeight;
                capsule.radius = ccRadius;
                capsule.center = ccCenter;
            }

            // Désactiver l'input sur les proxies
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health._currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (isDead || animator == null) return;

        if (newValue <= 0f)
        {
            isDead = true;
            Debug.Log($"[Player {OwnerClientId}] DEATH - HP dropped from {oldValue} to {newValue}");
            animator.SetTrigger(deathHash);
            if (IsOwner)
            {
                inputActions.Player.Disable();
            }
            if (controller != null) controller.enabled = false;
            enabled = false;
        }
        else if (newValue < oldValue)
        {
            Debug.Log($"[Player {OwnerClientId}] HIT - Took {oldValue - newValue} damage ({oldValue} -> {newValue} HP)");
            animator.SetTrigger(hitHash);
        }
    }

    void Update()
    {
        if (IsOwner)
        { 
            HandleGravity();
            if (PauseMenuManager.isPaused) return;
            // Lire l'input de mouvement chaque frame
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            HandleGroundCheck();
            HandleMovement();

            // Reset attaque après cooldown
            if (isAttacking && Time.time - lastAttackTime >= currentAttackDuration)
            {
                isAttacking = false;
                if (attackType == AttackType.Melee && weaponHitbox != null) weaponHitbox.DisableHitbox();
            }

            // Synchroniser l'état d'animation sur le réseau
            networkAnimSpeed.Value = moveInput.magnitude;
            networkIsGrounded.Value = isGrounded;
            networkIsFalling.Value = velocity.y < -1f && !isGrounded;
        }

        // Mettre à jour les animations sur TOUS les clients
        UpdateAnimations();
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void HandleMovement()
    {
        if (moveInput.magnitude < 0.01f) return;

        // Utiliser la caméra du joueur local, pas Camera.main (qui pourrait pointer vers un autre joueur)
        Transform cam = (playerCamera != null) ? playerCamera.transform : Camera.main.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMoveDirection = forward * moveInput.y + right * moveInput.x;

        if (desiredMoveDirection.magnitude > 1f)
            desiredMoveDirection.Normalize();

        if (desiredMoveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            float rotationSmoothness = rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness);
        }

        controller.Move(desiredMoveDirection * moveSpeed * Time.deltaTime);
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (isGrounded)
        {
            Debug.Log($"[Player {OwnerClientId}] JUMP - Force: {jumpForce}");
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetTrigger(jumpHash);
            // Synchroniser le trigger de saut aux autres clients
            JumpServerRpc();
        }
    }

    // Le owner envoie au serveur qu'il a sauté
    [ServerRpc]
    private void JumpServerRpc()
    {
        JumpClientRpc();
    }

    // Le serveur broadcast le saut à tous les clients
    [ClientRpc]
    private void JumpClientRpc()
    {
        // Ne pas re-trigger sur le owner (déjà fait localement)
        if (!IsOwner)
        {
            animator.SetTrigger(jumpHash);
        }
    }

    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isAttacking) return;
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (PauseMenuManager.isPaused) return;

        lastAttackTime = Time.time;
        currentAttackDuration = attackCooldown;
        isAttacking = true;
        Debug.Log($"[Player {OwnerClientId}] ATTACK ({attackType}) - Cooldown: {attackCooldown}s");

        if (attackType == AttackType.Melee)
        {
            if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        }
        else if (attackType == AttackType.Ranged)
        {
            ShootProjectile();
        }

        animator.SetTrigger(attackHash);
        AttackServerRpc();
    }

    // Appelé par un Animation Event à la fin de l'animation d'attaque
    public void OnAttackEnd()
    {
        isAttacking = false;
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position + Vector3.up * 0.8f;
        Vector3 direction = transform.forward;

        // Tirer vers le centre de la caméra (visée 3ème personne)
        Transform cam = (playerCamera != null) ? playerCamera.transform : Camera.main.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 100f))
        {
            direction = (hit.point - spawnPos).normalized;
        }
        else
        {
            direction = (cam.position + cam.forward * 100f - spawnPos).normalized;
        }

        ShootServerRpc(spawnPos, Quaternion.LookRotation(direction));
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Quaternion rotation)
    {
        GameObject proj = Instantiate(projectilePrefab, spawnPos, rotation);
        proj.GetComponent<NetworkObject>()?.Spawn();
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        AttackClientRpc();
    }

    [ClientRpc]
    private void AttackClientRpc()
    {
        if (!IsOwner)
        {
            animator.SetTrigger(attackHash);
        }
    }

    // --- Special 1 (Ability) ---
    void OnSpecial1Performed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isAttacking) return;
        if (ability == null) return;
        if (!ability.IsReady) return;

        Debug.Log($"[Player {OwnerClientId}] SPECIAL1 (Ability) - LockDuration: {ability.AttackLockDuration}s");
        ability.Activate();

        isAttacking = true;
        lastAttackTime = Time.time;
        currentAttackDuration = ability.AttackLockDuration;

        if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        animator.SetTrigger(special1Hash);

        Special1ServerRpc();
    }

    [ServerRpc]
    private void Special1ServerRpc()
    {
        Special1ClientRpc();
    }

    [ClientRpc]
    private void Special1ClientRpc()
    {
        if (!IsOwner)
        {
            animator.SetTrigger(special1Hash);
        }
    }

    // --- Special 2 (Ultimate) ---
    void OnSpecial2Performed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isAttacking) return;
        if (ultimate == null) return;
        if (!ultimate.IsReady)
        {
            return;
        }

        Debug.Log($"[Player {OwnerClientId}] SPECIAL2 (Ultimate) - LockDuration: {ultimate.AttackLockDuration}s");
        ultimate.Activate();

        isAttacking = true;
        lastAttackTime = Time.time;
        currentAttackDuration = ultimate.AttackLockDuration;

        if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        animator.SetTrigger(special2Hash);
        Special2ServerRpc();
    }

    [ServerRpc]
    private void Special2ServerRpc()
    {
        Special2ClientRpc();
    }

    [ClientRpc]
    private void Special2ClientRpc()
    {
        if (!IsOwner)
        {
            animator.SetTrigger(special2Hash);
        }
    }

    void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (IsOwner)
        {
            // Le joueur local utilise directement ses valeurs locales
            animator.SetFloat(speedHash, moveInput.magnitude);
            animator.SetBool(isGroundedHash, isGrounded);
            animator.SetBool(fallHash, velocity.y < -1f && !isGrounded);
        }
        else
        {
            // Les proxies utilisent les valeurs synchronisées via le réseau
            animator.SetFloat(speedHash, networkAnimSpeed.Value);
            animator.SetBool(isGroundedHash, networkIsGrounded.Value);
            animator.SetBool(fallHash, networkIsFalling.Value);
        }
    }
}
