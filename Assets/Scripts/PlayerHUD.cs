using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    private int playerLayer;
    private Health healthScript;
    private PlayerStats statsScript;
    private CastleInit castleScript;
    public Image healthBar;
    public Image castleBar;
    public Image bugBar;
    public TextMeshProUGUI moneyText;
    [SerializeField] private GameObject castle;

    void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }
    
    void Update()
    {
        if (!Camera.main) return;

        Transform parent = Camera.main.transform.parent;

        if (!parent || parent.gameObject.layer != playerLayer) return;

        if (!healthScript)
            healthScript = parent.GetComponent<Health>();

        if (!statsScript)
            statsScript = parent.GetComponent<PlayerStats>();

        if (!castleScript)
            castleScript = castle.GetComponent<CastleInit>();

        if (healthScript) {
            float current = healthScript._currentHealth.Value;
            float max = healthScript.maxHealth;
            
            if (healthBar) healthBar.fillAmount = current / max;
        }

        if (statsScript) {
            int goldValue = statsScript.Gold.Value; 

            if (moneyText) moneyText.text = goldValue.ToString();
        }

        if (castleScript) {
            float current = castleScript.health.Value;
            float max = castleScript.maxHealth.Value;
            
            if (castleBar) castleBar.fillAmount = current / max;
        }
    }
}
