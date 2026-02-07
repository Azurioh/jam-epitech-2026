using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // <-- Ajoute ceci !

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public static bool isPaused = false;

    void Update()
    {
        // La nouvelle façon de détecter une touche (sans configurer d'Action)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    // Le reste des fonctions (Resume, Pause, Quit) reste identique
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
