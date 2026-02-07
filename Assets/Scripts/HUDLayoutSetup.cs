using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDLayoutSetup : MonoBehaviour
{
    [Header("Assign Elements")]
    public RectTransform healthBarRect;
    public RectTransform healthIconRect; // Nouvelle icône
    public RectTransform chaosBarRect;
    public RectTransform goldTextRect;
    public RectTransform goldIconRect;   // Nouvelle icône
    
    [Header("Layout Settings")]
    public float padding = 20f;
    public float iconSize = 40f; // Taille des icônes
    public float healthBarWidth = 300f;
    public float healthBarHeight = 30f;
    
    public float chaosBarWidth = 400f;
    public float chaosBarHeight = 15f;

    [ContextMenu("Apply Cyber Layout")]
    public void ApplyLayout()
    {
        // 1. Setup Health Bar (Bottom Left)
        if (healthBarRect != null)
        {
            healthBarRect.anchorMin = new Vector2(0, 0);
            healthBarRect.anchorMax = new Vector2(0, 0);
            healthBarRect.pivot = new Vector2(0, 0);
            
            // Decale la barre pour laisser la place à l'icone
            float xPos = padding;
            if (healthIconRect != null) xPos += iconSize + 10f; 

            healthBarRect.anchoredPosition = new Vector2(xPos, padding);
            healthBarRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            
            healthBarRect.localRotation = Quaternion.identity; 
        }

        // 1b. Setup Health Icon
        if (healthIconRect != null)
        {
             healthIconRect.anchorMin = new Vector2(0, 0);
             healthIconRect.anchorMax = new Vector2(0, 0);
             healthIconRect.pivot = new Vector2(0, 0);
             healthIconRect.anchoredPosition = new Vector2(padding, padding);
             healthIconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        // 2. Setup Chaos Bar (Top Center)
        if (chaosBarRect != null)
        {
            chaosBarRect.anchorMin = new Vector2(0.5f, 1);
            chaosBarRect.anchorMax = new Vector2(0.5f, 1);
            chaosBarRect.pivot = new Vector2(0.5f, 1);
            
            chaosBarRect.anchoredPosition = new Vector2(0, -padding);
            chaosBarRect.sizeDelta = new Vector2(chaosBarWidth, chaosBarHeight);
        }

        // 3. Setup Gold Text (Top Right)
        if (goldTextRect != null)
        {
            goldTextRect.anchorMin = new Vector2(1, 1);
            goldTextRect.anchorMax = new Vector2(1, 1);
            goldTextRect.pivot = new Vector2(1, 1);
            
            goldTextRect.anchoredPosition = new Vector2(-padding, -padding);
            
            var tmp = goldTextRect.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.Right;
                tmp.fontSize = 36;
            }
        }

        // 3b. Setup Gold Icon
        if (goldIconRect != null)
        {
             goldIconRect.anchorMin = new Vector2(1, 1);
             goldIconRect.anchorMax = new Vector2(1, 1);
             goldIconRect.pivot = new Vector2(1, 1);
             
             // On positionne l'icone à gauche du texte (approximatif)
             float textWidthApprox = 200f; 
             goldIconRect.anchoredPosition = new Vector2(-padding - textWidthApprox, -padding);
             goldIconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        Debug.Log("Layout Updated with Icons!");
    }
}
