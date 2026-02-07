using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("UI Elements")]
    [Header("UI Elements")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI goldText;
    public GameObject crosshair;
    
    [Header("Chaos System")]
    public Slider stabilityBar;

    void Awake()
    {
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
        if (healthBar != null)
        {
            healthBar.maxValue = 100;
            healthBar.value = health;
            
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (health > 50) fillImage.color = Color.green;
                else if (health > 20) fillImage.color = Color.yellow;
                else fillImage.color = Color.red;
            }
        }

        if (healthText != null)
        {
            healthText.text = health + " HP";
        }
    }

    public void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = "Gold: " + gold.ToString();
        }
    }

    public void UpdateStability(float stability)
    {
        if (stabilityBar != null)
        {
            stabilityBar.maxValue = 100f;
            stabilityBar.value = stability;
        }
    }

    public void ToggleCrosshair(bool state)
    {
        if (crosshair != null)
        {
            crosshair.SetActive(state);
        }
    }
}
