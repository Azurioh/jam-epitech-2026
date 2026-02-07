using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineMouseController : MonoBehaviour
{
    [SerializeField] private Transform target; // Ton player
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = 5f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    private float currentX = 15f;
    private float currentY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (PauseMenuManager.isPaused) return;
        if (target == null) return;
        
        // Input souris avec New Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        float mouseX = mouseDelta.x * mouseSensitivity * 0.02f; // 0.02f pour compenser le delta
        float mouseY = mouseDelta.y * mouseSensitivity * 0.02f;
        
        currentY += mouseX;
        currentX -= mouseY;
        currentX = Mathf.Clamp(currentX, minVerticalAngle, maxVerticalAngle);
        
        // Position suit le player, rotation indépendante
        transform.position = target.position;
        transform.rotation = Quaternion.Euler(currentX, currentY, 0);
        
        // ESC
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked 
                ? CursorLockMode.None 
                : CursorLockMode.Locked;
        }
    }
}
