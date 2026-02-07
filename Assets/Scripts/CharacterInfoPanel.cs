using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau d'affichage des informations du personnage sélectionné
/// Affiche: bio, stats, et compétences
/// </summary>
public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Bio Section")]
    [SerializeField] private TextMeshProUGUI bioText;

    [Header("Stats Section")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;

    [Header("Abilities Section")]
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject abilityRowPrefab;

    [Header("Style")]
    [SerializeField] private Color healthColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color damageColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color speedColor = new Color(0.3f, 0.8f, 0.9f);
    [SerializeField] private Color attackSpeedColor = new Color(0.9f, 0.9f, 0.4f);

    private void Start()
    {
        // Créer les prefabs au runtime s'ils ne sont pas assignés
        if (statRowPrefab == null) CreateDefaultStatRowPrefab();
        if (abilityRowPrefab == null) CreateDefaultAbilityRowPrefab();
    }

    /// <summary>
    /// Met à jour le panneau avec les données du personnage
    /// </summary>
    public void UpdateCharacterInfo(CharacterData data)
    {
        if (data == null) return;

        // Bio
        if (bioText != null)
        {
            bioText.text = data.biography;
        }

        // Stats
        UpdateStats(data);

        // Abilities
        UpdateAbilities(data);
    }

    private void UpdateStats(CharacterData data)
    {
        if (statsContainer == null) return;

        // Nettoyer les anciennes stats
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }

        // Créer les nouvelles stats (version compacte)
        CreateStatRow("VIE", data.health.ToString(), healthColor, data.health / 300f);
        CreateStatRow("ATK", data.damage.ToString(), damageColor, data.damage / 50f);
        CreateStatRow("ATS", data.attackSpeed.ToString("F1"), attackSpeedColor, data.attackSpeed / 3f);
        CreateStatRow("MOV", data.moveSpeed.ToString("F1"), speedColor, data.moveSpeed / 10f);
    }

    private void CreateStatRow(string statName, string value, Color barColor, float fillAmount)
    {
        if (statsContainer == null) return;

        GameObject row = new GameObject(statName);
        row.transform.SetParent(statsContainer, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(250, 14);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Nom de la stat
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = statName;
        nameText.fontSize = 10;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.95f, 0.9f, 0.8f);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(32, 14);

        // Barre de progression (fond sombre)
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(row.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.08f, 0.06f, 0.04f, 0.9f);
        
        Outline barOutline = barBg.AddComponent<Outline>();
        barOutline.effectColor = new Color(0.3f, 0.25f, 0.15f, 0.8f);
        barOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.sizeDelta = new Vector2(155, 12);

        // Barre de progression (remplissage coloré)
        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = barColor;
        RectTransform barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0, 0.1f);
        barFillRect.anchorMax = new Vector2(Mathf.Clamp01(fillAmount), 0.9f);
        barFillRect.offsetMin = new Vector2(2, 0);
        barFillRect.offsetMax = new Vector2(-2, 0);

        // Valeur numérique
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.fontSize = 10;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = new Color(1f, 0.95f, 0.85f);
        valueText.alignment = TextAlignmentOptions.Right;
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(35, 14);
    }

    private void UpdateAbilities(CharacterData data)
    {
        if (abilitiesContainer == null) return;

        // Nettoyer les anciennes abilities
        foreach (Transform child in abilitiesContainer)
        {
            Destroy(child.gameObject);
        }

        if (data.abilities == null) return;

        foreach (var ability in data.abilities)
        {
            CreateAbilityRow(ability);
        }
    }

    private void CreateAbilityRow(AbilityInfo ability)
    {
        if (abilitiesContainer == null) return;

        GameObject row = new GameObject(ability.abilityName);
        row.transform.SetParent(abilitiesContainer, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(240, 32);

        VerticalLayoutGroup vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.padding = new RectOffset(4, 4, 2, 2);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;

        // Simple background (no border for cleaner look)
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.03f, 0.6f);

        // Header (Nom + Cooldown)
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(row.transform, false);
        
        HorizontalLayoutGroup headerHlg = headerObj.AddComponent<HorizontalLayoutGroup>();
        headerHlg.childControlWidth = false;
        headerHlg.childForceExpandWidth = false;
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(238, 14);

        // Nom
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(headerObj.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = ability.abilityName;
        nameText.fontSize = 11;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 0.9f, 0.55f);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(175, 14);

        // Cooldown badge
        GameObject cdObj = new GameObject("Cooldown");
        cdObj.transform.SetParent(headerObj.transform, false);
        TextMeshProUGUI cdText = cdObj.AddComponent<TextMeshProUGUI>();
        cdText.text = $"{ability.cooldown:F0}s";
        cdText.fontSize = 10;
        cdText.color = new Color(0.6f, 0.75f, 0.9f);
        cdText.alignment = TextAlignmentOptions.Right;
        RectTransform cdRect = cdObj.GetComponent<RectTransform>();
        cdRect.sizeDelta = new Vector2(50, 14);

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = ability.description;
        descText.fontSize = 8;
        descText.color = new Color(0.75f, 0.7f, 0.6f);
        descText.enableWordWrapping = true;
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(238, 16);
    }

    private void CreateDefaultStatRowPrefab()
    {
        statRowPrefab = new GameObject("StatRowPrefab");
        statRowPrefab.SetActive(false);
    }

    private void CreateDefaultAbilityRowPrefab()
    {
        abilityRowPrefab = new GameObject("AbilityRowPrefab");
        abilityRowPrefab.SetActive(false);
    }
}
