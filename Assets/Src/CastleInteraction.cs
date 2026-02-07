using UnityEngine;
using UnityEngine.InputSystem;

public class CastleInteraction : MonoBehaviour
{
    public GameObject uiPanel;
    public string playerTag = "Player";

    private bool playerInRange = false;
    private bool uiOpen = false;

    void Update()
    {
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleUI();
        }

        if (uiOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseUI();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger Enter détecté : {other.gameObject.name} avec tag : {other.tag}");

        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            Debug.Log($"✓ JOUEUR DANS LA ZONE : {other.gameObject.name} - Appuyez sur E pour interagir");
        }
        else
        {
            Debug.Log($"✗ Pas un joueur - Tag attendu: '{playerTag}', Tag reçu: '{other.tag}'");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger Exit détecté : {other.gameObject.name}");

        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            Debug.Log($"✓ JOUEUR SORTI DE LA ZONE : {other.gameObject.name}");
            CloseUI(); // Ferme l'UI automatiquement si le joueur s'éloigne
        }
    }

    void ToggleUI()
    {
        if (uiOpen)
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    public void OpenUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
            uiOpen = true;
            Debug.Log("Interface du château ouverte !");
            
            // Si tu veux mettre le jeu en pause
            // Time.timeScale = 0f;
        }
    }

    public void CloseUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
            uiOpen = false;
            Debug.Log("Interface du château fermée !");
            
            // Si tu as mis en pause
            // Time.timeScale = 1f;
        }
    }

}
