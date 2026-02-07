using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    [SerializeField] private TextMeshProUGUI codeTextDisplay;
    public static bool canPaused = false;
    public static bool isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }

    void Update()
    {
        if (canPaused && Keyboard.current.escapeKey.wasPressedThisFrame) {
            if (isPaused)
                Resume();
            else
                OpenMenu();
        }

        if (isPaused && codeTextDisplay != null) {
            codeTextDisplay.text = "Code: " + NetworkUI.joinCode;
        }
    }

    public void Resume()
    {
        Debug.Log("Resuming game...");
        pauseMenuUI.SetActive(false);
        isPaused = false;

        // Désactiver la caméra de la scène quand on reprend le jeu
        NetworkUI.DisableSceneMainCameraStatic();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CopyCode()
    {
        GUIUtility.systemCopyBuffer = NetworkUI.joinCode;
    }

    public void Quit()
    {
        Application.Quit();
    }

    void OpenMenu()
    {
        pauseMenuUI.SetActive(true);
        isPaused = true;

        // Réactiver la caméra de la scène pour le menu pause
        NetworkUI.EnableSceneMainCamera();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
