using UnityEngine;
using TMPro; // Important pour TextMeshPro
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI goldText;
    public GameObject crosshair; // Peut être une Image au centre

    void Awake()
    {
        // Singleton simple : s'assure qu'il n'y en a qu'un et qu'il est accessible partout
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateHealth(int health)
    {
        if (healthText != null)
        {
            healthText.text = "HP: " + health.ToString();
            
            // Petit plus : changer la couleur si low HP
            if (health < 30) healthText.color = Color.red;
            else healthText.color = Color.white;
        }
    }

    public void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = "Gold: " + gold.ToString();
        }
    }

    // Pour activer/désactiver le crosshair si besoin (ex: dans les menus)
    public void ToggleCrosshair(bool state)
    {
        if (crosshair != null)
        {
            crosshair.SetActive(state);
        }
    }
}
