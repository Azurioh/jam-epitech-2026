using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ajoute ce script au Canvas de sélection de personnage.
/// Il crée automatiquement le panel d'informations au démarrage.
/// </summary>
public class CharacterInfoPanelSetup : MonoBehaviour
{
    [Header("Style Settings")]
    public Color panelBackgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.92f);
    public Color borderColor = new Color(0.6f, 0.45f, 0.25f, 1f);
    public Color titleColor = new Color(1f, 0.85f, 0.5f);
    public Color bioTextColor = new Color(0.85f, 0.8f, 0.7f);

    private CharacterInfoPanel infoPanel;
    private CharacterSelectorUI selectorUI;

    void Start()
    {
        // Trouver le CharacterSelectorUI
        selectorUI = FindObjectOfType<CharacterSelectorUI>();

        // Créer le panel
        CreateInfoPanel();

        // Lier au selector
        if (selectorUI != null && infoPanel != null)
        {
            selectorUI.infoPanel = infoPanel;
            Debug.Log("✅ CharacterInfoPanelSetup: Panel linked to CharacterSelectorUI!");
        }
    }

    private void CreateInfoPanel()
    {
        // === MAIN PANEL ===
        GameObject panelObj = new GameObject("CharacterInfoPanel");
        panelObj.transform.SetParent(transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0.5f);
        panelRect.anchorMax = new Vector2(1, 0.5f);
        panelRect.pivot = new Vector2(1, 0.5f);
        panelRect.anchoredPosition = new Vector2(-30, 0);
        panelRect.sizeDelta = new Vector2(320, 500);

        // Background avec bordure
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = panelBackgroundColor;

        // Bordure (outline effect via additional image)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(panelObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);
        borderRect.SetAsFirstSibling();
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = borderColor;

        // Layout
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // === BIO SECTION ===
        GameObject bioSection = CreateSection(panelObj.transform, "Bio", 80);
        TextMeshProUGUI bioText = CreateInfoText(bioSection.transform, "biography_text");
        bioText.fontSize = 13;
        bioText.color = bioTextColor;
        bioText.fontStyle = FontStyles.Italic;
        bioText.enableWordWrapping = true;
        bioText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform bioTextRect = bioText.GetComponent<RectTransform>();
        bioTextRect.sizeDelta = new Vector2(280, 60);

        // === STATS SECTION ===
        GameObject statsSection = CreateSection(panelObj.transform, "⚔️ Stats", 160);
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(statsSection.transform, false);
        RectTransform statsContainerRect = statsContainer.AddComponent<RectTransform>();
        statsContainerRect.anchorMin = Vector2.zero;
        statsContainerRect.anchorMax = Vector2.one;
        statsContainerRect.offsetMin = new Vector2(5, 5);
        statsContainerRect.offsetMax = new Vector2(-5, -25);

        VerticalLayoutGroup statsVlg = statsContainer.AddComponent<VerticalLayoutGroup>();
        statsVlg.spacing = 4;
        statsVlg.childAlignment = TextAnchor.UpperLeft;
        statsVlg.childControlWidth = true;
        statsVlg.childControlHeight = false;

        // === ABILITIES SECTION ===
        GameObject abilitiesSection = CreateSection(panelObj.transform, "✨ Compétences", 180);
        GameObject abilitiesContainer = new GameObject("AbilitiesContainer");
        abilitiesContainer.transform.SetParent(abilitiesSection.transform, false);
        RectTransform abilitiesContainerRect = abilitiesContainer.AddComponent<RectTransform>();
        abilitiesContainerRect.anchorMin = Vector2.zero;
        abilitiesContainerRect.anchorMax = Vector2.one;
        abilitiesContainerRect.offsetMin = new Vector2(5, 5);
        abilitiesContainerRect.offsetMax = new Vector2(-5, -25);

        VerticalLayoutGroup abilitiesVlg = abilitiesContainer.AddComponent<VerticalLayoutGroup>();
        abilitiesVlg.spacing = 6;
        abilitiesVlg.childAlignment = TextAnchor.UpperLeft;
        abilitiesVlg.childControlWidth = true;
        abilitiesVlg.childControlHeight = false;

        // === ADD COMPONENT ===
        infoPanel = panelObj.AddComponent<CharacterInfoPanel>();

        // Set references via reflection (since fields are serialized)
        var bioTextField = typeof(CharacterInfoPanel).GetField("bioText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var statsContainerField = typeof(CharacterInfoPanel).GetField("statsContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var abilitiesContainerField = typeof(CharacterInfoPanel).GetField("abilitiesContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (bioTextField != null) bioTextField.SetValue(infoPanel, bioText);
        if (statsContainerField != null) statsContainerField.SetValue(infoPanel, statsContainer.transform);
        if (abilitiesContainerField != null) abilitiesContainerField.SetValue(infoPanel, abilitiesContainer.transform);

        Debug.Log("✅ CharacterInfoPanelSetup: Panel created with Bio, Stats, and Abilities sections!");
    }

    private GameObject CreateSection(Transform parent, string title, float height)
    {
        GameObject section = new GameObject(title + "_Section");
        section.transform.SetParent(parent, false);

        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(290, height);

        // Background
        Image sectionBg = section.AddComponent<Image>();
        sectionBg.color = new Color(0.08f, 0.05f, 0.03f, 0.6f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(section.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = titleColor;
        titleText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -5);
        titleRect.sizeDelta = new Vector2(-10, 22);

        return section;
    }

    private TextMeshProUGUI CreateInfoText(Transform parent, string name)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(8, 8);
        textRect.offsetMax = new Vector2(-8, -28);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        return text;
    }
}
