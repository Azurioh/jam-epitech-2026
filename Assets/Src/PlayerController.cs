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

    // Attack
    private float lastAttackTime = -999f;
    private bool isAttacking;

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
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Disable();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        var vars = Variables.Object(this.gameObject);
        moveSpeed = vars.Get<float>("MOVE_SPEED");
        rotationSpeed = vars.Get<float>("ROTATION_SPEED");
        attackCooldown = vars.Get<float>("ATTACK_COOLDOWN");
        jumpForce = vars.Get<float>("JUMP_FORCE");
        gravity = vars.Get<float>("GRAVITY");
        groundCheck = vars.Get<Transform>("GROUND_CHECK");

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

    void Update()
    {
        if (IsOwner)
        {
            // Lire l'input de mouvement chaque frame
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            HandleGroundCheck();
            HandleMovement();
            HandleGravity();

            // Reset attaque après cooldown
            if (isAttacking && Time.time - lastAttackTime >= attackCooldown)
                isAttacking = false;

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
        isAttacking = true;
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
