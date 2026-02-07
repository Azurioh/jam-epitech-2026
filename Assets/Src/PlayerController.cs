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

    [Header("Attack")]
    [SerializeField] private float attackCooldown;
    [SerializeField] private DamageOnContact weaponHitbox;

    [Header("Special Attacks")]
    [SerializeField] private float special1Cooldown;
    [SerializeField] private float special2Cooldown;

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
    private LagEffectReceiver lagReceiver;

    // Special attacks
    private float lastSpecial1Time = -999f;
    private float lastSpecial2Time = -999f;

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
        lagReceiver = GetComponent<LagEffectReceiver>();
    }

    void OnEnable()
    {
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
        special1Cooldown = vars.Get<float>("SPECIAL_COOLDOWN_1");
        special2Cooldown = vars.Get<float>("SPECIAL_COOLDOWN_2");

        // Téléporter au spawn point
        if (IsServer)
        {
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
            animator.SetTrigger(hitHash);
        }
    }

    void Update()
    {
        bool isLagged = lagReceiver != null && lagReceiver.IsLagged;
        float lagMultiplier = isLagged ? lagReceiver.GetSpeedMultiplier() : 1f;
        float lagStep = Time.deltaTime;
        bool allowLagStep = !isLagged || lagReceiver.TryConsumeLagStep(out lagStep);
        float deltaTime = isLagged ? lagStep : Time.deltaTime;

        if (animator != null)
        {
            if (isLagged)
            {
                animator.speed = 0f;
                if (lagReceiver.TryGetAnimationDelta(out float animDelta))
                {
                    animator.Update(animDelta);
                }
            }
            else
            {
                animator.speed = 1f;
            }
        }

        if (IsOwner)
        {
            if (allowLagStep)
            {
                // Lire l'input de mouvement au tick de lag
                moveInput = inputActions.Player.Move.ReadValue<Vector2>();
                moveInput = Vector2.ClampMagnitude(moveInput, 1f);

                HandleGroundCheck();
                HandleMovement(deltaTime, lagMultiplier);
                HandleGravity(deltaTime, lagMultiplier);
            }

            // Reset attaque après cooldown
            if (isAttacking && Time.time - lastAttackTime >= currentAttackDuration)
            {
                isAttacking = false;
                if (weaponHitbox != null) weaponHitbox.DisableHitbox();
            }

            // Synchroniser l'état d'animation sur le réseau
            if (allowLagStep)
            {
                networkAnimSpeed.Value = moveInput.magnitude;
                networkIsGrounded.Value = isGrounded;
                networkIsFalling.Value = velocity.y < -1f && !isGrounded;
            }
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

    void HandleMovement(float deltaTime, float speedMultiplier)
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
            float rotationSmoothness = rotationSpeed * speedMultiplier * deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness);
        }

        controller.Move(desiredMoveDirection * (moveSpeed * speedMultiplier) * deltaTime);
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (isGrounded)
        {
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

        lastAttackTime = Time.time;
        currentAttackDuration = attackCooldown;
        isAttacking = true;
        Debug.Log("Attack");
        if (weaponHitbox != null) weaponHitbox.EnableHitbox();
        animator.SetTrigger(attackHash);
        AttackServerRpc();
    }

    // Appelé par un Animation Event à la fin de l'animation d'attaque
    public void OnAttackEnd()
    {
        isAttacking = false;
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

    // --- Special 1 ---
    void OnSpecial1Performed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isAttacking) return;
        if (Time.time - lastSpecial1Time < special1Cooldown) return;

        lastSpecial1Time = Time.time;
        isAttacking = true;
        lastAttackTime = Time.time;
        currentAttackDuration = special1Cooldown;
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

    // --- Special 2 ---
    void OnSpecial2Performed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (isAttacking) return;
        if (Time.time - lastSpecial2Time < special2Cooldown) return;

        lastSpecial2Time = Time.time;
        isAttacking = true;
        lastAttackTime = Time.time;
        currentAttackDuration = special2Cooldown;
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

    void HandleGravity(float deltaTime, float speedMultiplier)
    {
        velocity.y += gravity * speedMultiplier * deltaTime;
        controller.Move(velocity * speedMultiplier * deltaTime);
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
