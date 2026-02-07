using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectorUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI characterNameText;
    public CharacterSelector selector;

    [Header("Character Info Panel")]
    public CharacterInfoPanel infoPanel;
    public CharacterData[] characterDataList;

    [Header("Panel Settings")]
    public bool autoCreatePanel = true;

    private int lastIndex = -1;

    void Start()
    {
        // Auto-generate character data if not assigned
        if (characterDataList == null || characterDataList.Length == 0)
        {
            GenerateDefaultCharacterData();
        }

        // Auto-create info panel if not assigned
        if (autoCreatePanel && infoPanel == null)
        {
            CreateInfoPanel();
        }

        // Force initial update
        lastIndex = -1;
        
        // Force immediate update
        if (infoPanel != null && characterDataList != null && characterDataList.Length > 0)
        {
            infoPanel.UpdateCharacterInfo(characterDataList[0]);
        }
    }

    private void CreateInfoPanel()
    {
        // Find Canvas in parent hierarchy
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("❌ CharacterSelectorUI: No Canvas found! Cannot create panel.");
            return;
        }

        Debug.Log("🎨 CharacterSelectorUI: Creating split info panels on Canvas: " + canvas.name);

        // === LEFT PANEL (Stats) ===
        GameObject leftPanel = CreateBottomPanel(canvas.transform, "LeftInfoPanel", true);
        
        // Stats Section (4 stats)
        GameObject statsSection = CreateSection(leftPanel.transform, "Stats", 95);
        Transform statsContainer = CreateContainer(statsSection.transform);

        // === RIGHT PANEL (Skills) ===
        GameObject rightPanel = CreateBottomPanel(canvas.transform, "RightInfoPanel", false);
        
        // Skills Section (2 abilities)
        GameObject abilitiesSection = CreateSection(rightPanel.transform, "Skills", 95);
        Transform abilitiesContainer = CreateContainer(abilitiesSection.transform);

        // Bio text placeholder (not displayed, but needed for component)
        TextMeshProUGUI bioText = null;

        // === DUMMY PARENT FOR CharacterInfoPanel ===
        GameObject infoPanelHolder = new GameObject("CharacterInfoPanelHolder");
        infoPanelHolder.transform.SetParent(canvas.transform, false);
        
        infoPanel = infoPanelHolder.AddComponent<CharacterInfoPanel>();

        // Use reflection to set serialized fields
        SetPrivateField(infoPanel, "bioText", bioText);
        SetPrivateField(infoPanel, "statsContainer", statsContainer);
        SetPrivateField(infoPanel, "abilitiesContainer", abilitiesContainer);

        Debug.Log("✅ CharacterSelectorUI: Split info panels created successfully!");
    }

    private GameObject CreateBottomPanel(Transform parent, string name, bool isLeft)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        
        if (isLeft)
        {
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(10, 10);
        }
        else
        {
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);
            panelRect.anchoredPosition = new Vector2(-10, 10);
        }
        
        panelRect.sizeDelta = new Vector2(280, 120);

        // Main background - parchment color
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.22f, 0.16f, 0.1f, 0.95f);

        // Double border effect for depth
        Outline outerBorder = panelObj.AddComponent<Outline>();
        outerBorder.effectColor = new Color(0.4f, 0.3f, 0.15f, 1f);
        outerBorder.effectDistance = new Vector2(3, 3);

        // Inner highlight
        Shadow innerShadow = panelObj.AddComponent<Shadow>();
        innerShadow.effectColor = new Color(0.7f, 0.55f, 0.3f, 0.8f);
        innerShadow.effectDistance = new Vector2(-1, -1);

        // Layout
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return panelObj;
    }

    private GameObject CreateSection(Transform parent, string title, float height)
    {
        GameObject section = new GameObject(title.Replace(" ", "_"));
        section.transform.SetParent(parent, false);

        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(260, height);

        // Section background - slightly darker
        Image sectionBg = section.AddComponent<Image>();
        sectionBg.color = new Color(0.12f, 0.08f, 0.04f, 0.7f);

        // Title bar
        GameObject titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(section.transform, false);
        
        RectTransform titleBarRect = titleBar.AddComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0, 1);
        titleBarRect.anchorMax = new Vector2(1, 1);
        titleBarRect.pivot = new Vector2(0.5f, 1);
        titleBarRect.anchoredPosition = Vector2.zero;
        titleBarRect.sizeDelta = new Vector2(0, 22);

        Image titleBarBg = titleBar.AddComponent<Image>();
        titleBarBg.color = new Color(0.35f, 0.25f, 0.12f, 0.9f);

        // Title text (clean, no checkbox)
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(titleBar.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 13;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.92f, 0.7f);
        titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        return section;
    }

    private TextMeshProUGUI CreateTextInSection(Transform parent, int fontSize, Color color, FontStyles style, float height)
    {
        GameObject textObj = new GameObject("ContentText");
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(8, 6);
        textRect.offsetMax = new Vector2(-8, -24);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.TopLeft;

        return text;
    }

    private Transform CreateContainer(Transform parent)
    {
        GameObject container = new GameObject("Container");
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(6, 4);
        containerRect.offsetMax = new Vector2(-6, -24);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return container.transform;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private void GenerateDefaultCharacterData()
    {
        characterDataList = new CharacterData[5];

        // ===== BARBARIAN =====
        characterDataList[0] = ScriptableObject.CreateInstance<CharacterData>();
        characterDataList[0].characterName = "Babarian";
        characterDataList[0].biography = "Un guerrier sauvage venu des terres du Nord, accompagné de son fidèle ours de combat. Sa rage légendaire le rend invincible sur le champ de bataille.";
        characterDataList[0].health = 180;
        characterDataList[0].damage = 35;
        characterDataList[0].attackSpeed = 0.9f;
        characterDataList[0].moveSpeed = 5.5f;
        characterDataList[0].abilities = new AbilityInfo[]
        {
            new AbilityInfo { abilityName = "Rage", description = "Augmente les dégâts de 50% pendant 4s.", cooldown = 6f },
            new AbilityInfo { abilityName = "Valkyrie", description = "Fait une toupie.", cooldown = 10f }
        };

        // ===== KNIGHT =====
        characterDataList[1] = ScriptableObject.CreateInstance<CharacterData>();
        characterDataList[1].characterName = "Knight";
        characterDataList[1].biography = "Un chevalier noble formé dans les meilleures académies royales. Son armure et bouclier le protègent de tous les dangers.";
        characterDataList[1].health = 200;
        characterDataList[1].damage = 25;
        characterDataList[1].attackSpeed = 1.0f;
        characterDataList[1].moveSpeed = 4.5f;
        characterDataList[1].abilities = new AbilityInfo[]
        {
            new AbilityInfo { abilityName = "Bouclier", description = "Bloque tous les dégâts pendant 3s.", cooldown = 5f },
            new AbilityInfo { abilityName = "Hulk", description = "Devient géant et augmente ses dégats et sa vie pendant 5s", cooldown = 8f }
        };

        // ===== MAGE =====
        characterDataList[2] = ScriptableObject.CreateInstance<CharacterData>();
        characterDataList[2].characterName = "Mage";
        characterDataList[2].biography = "Maître des arcanes mystiques, capable de manipuler les éléments. Son chapeau pointu cache une intelligence redoutable.";
        characterDataList[2].health = 100;
        characterDataList[2].damage = 45;
        characterDataList[2].attackSpeed = 0.7f;
        characterDataList[2].moveSpeed = 5.0f;
        characterDataList[2].abilities = new AbilityInfo[]
        {
            new AbilityInfo { abilityName = "??", description = "???", cooldown = 4f },
            new AbilityInfo { abilityName = "??", description = "???", cooldown = 10f },
        };

        // ===== RANGER =====
        characterDataList[3] = ScriptableObject.CreateInstance<CharacterData>();
        characterDataList[3].characterName = "Ranger";
        characterDataList[3].biography = "Un simple fermier devenu héros par nécessité. Ce qu'il manque en force, il le compense par sa débrouillardise.";
        characterDataList[3].health = 100;
        characterDataList[3].damage = 25;
        characterDataList[3].attackSpeed = 1.2f;
        characterDataList[3].moveSpeed = 6.0f;
        characterDataList[3].abilities = new AbilityInfo[]
        {
            new AbilityInfo { abilityName = "Flèche de feu", description = "Tire des flèches de feu pendant 6s augmentant ces dégats de 50%.", cooldown = 8f },
            new AbilityInfo { abilityName = "Pluie de flèche", description = "Tire une pluie de flèches", cooldown = 12f }
        };

        // ===== ROGUE =====
        characterDataList[4] = ScriptableObject.CreateInstance<CharacterData>();
        characterDataList[4].characterName = "Rogue";
        characterDataList[4].biography = "Un assassin silencieux maniant l'arc avec précision mortelle. Personne ne l'a jamais vu venir... ni repartir.";
        characterDataList[4].health = 90;
        characterDataList[4].damage = 40;
        characterDataList[4].attackSpeed = 1.5f;
        characterDataList[4].moveSpeed = 7.0f;
        characterDataList[4].abilities = new AbilityInfo[]
        {
            new AbilityInfo { abilityName = "Smoke", description = "Créer une zone de fumée durant 6s", cooldown = 8f },
            new AbilityInfo { abilityName = "Frenesy", description = "Augmente la vitesse d'attaque par 3 pendant 10s", cooldown = 15f },
        };

        Debug.Log("✅ CharacterSelectorUI: Default character data generated!");
    }

    void Update()
    {
        if (characterNameText != null && selector != null)
        {
            characterNameText.text = selector.GetCurrentCharacterName();
        }

        UpdateInfoPanel();
    }

    private void UpdateInfoPanel()
    {
        if (infoPanel == null || selector == null || characterDataList == null) return;

        int currentIndex = GetCurrentCharacterIndex();
        if (currentIndex != lastIndex && currentIndex >= 0 && currentIndex < characterDataList.Length)
        {
            lastIndex = currentIndex;
            if (characterDataList[currentIndex] != null)
            {
                infoPanel.UpdateCharacterInfo(characterDataList[currentIndex]);
            }
        }
    }

    private int GetCurrentCharacterIndex()
    {
        var field = typeof(CharacterSelector).GetField("_currentIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(selector);
        }
        return 0;
    }

    public void OnNextButton()
    {
        if (selector != null) selector.NextCharacter();
    }

    public void OnPreviousButton()
    {
        if (selector != null) selector.PreviousCharacter();
    }

    public void OnConfirmButton()
    {
        if (selector != null) selector.ConfirmSelection();
    }
}


