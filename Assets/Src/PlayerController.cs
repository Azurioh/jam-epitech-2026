using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    
    // Components
    private CharacterController controller;
    private Animator animator;
    private PlayerInputActions inputActions; // Référence à tes inputs
    
    // Input
    private Vector2 moveInput;
    
    // Physics
    private Vector3 velocity;
    public bool isGrounded;
    
    // Animation parameters
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int fallHash = Animator.StringToHash("IsFalling");

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJumpPerformed;
    }

    void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Lire l'input de mouvement chaque frame
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        
        HandleGroundCheck();
        HandleMovement();
        HandleGravity();
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
        
        Transform cam = Camera.main.transform;
        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Direction du mouvement
        Vector3 move = (forward * moveInput.y + right * moveInput.x).normalized;
        
        // ROTATION : Ne tourne QUE si on avance (pas en reculant/strafing)
        if (moveInput.y > 0.1f) // Seulement quand on appuie sur W (avancer)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Déplacement (toujours dans la direction, même en reculant)
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetTrigger(jumpHash);
        }
    }

    void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        float animSpeed = moveInput.magnitude;
        animator.SetFloat(speedHash, animSpeed);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(fallHash, velocity.y < -1f && !isGrounded);
    }
}
