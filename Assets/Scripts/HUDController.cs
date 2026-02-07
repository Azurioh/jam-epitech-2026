using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("UI Elements")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI goldText;
    public GameObject crosshair;

    [Header("Chaos System")]
    public Slider stabilityBar;
    
    [Header("Animation")]
    public HUDAnimator animator;

    private int _lastHealth = 100;
    private int _lastGold = 0;

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
        
        if (animator == null) animator = GetComponent<HUDAnimator>();
    }

    public void UpdateHealth(int health)
    {
        if (health < _lastHealth)
        {
            if (animator != null && healthBar != null)
                animator.ShakeObject(healthBar.GetComponent<RectTransform>(), 0.3f, 5f);
        }
        _lastHealth = health;

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
            healthText.text = "HP: " + health.ToString();

            // Petit plus : changer la couleur si low HP
            if (health < 30) healthText.color = Color.red;
            else healthText.color = Color.white;
        }
    }

    public void UpdateGold(int gold)
    {
        if (gold > _lastGold)
        {
            if (animator != null && goldText != null)
                animator.PulseObject(goldText.rectTransform, 0.2f, 1.5f);
        }
        _lastGold = gold;

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
