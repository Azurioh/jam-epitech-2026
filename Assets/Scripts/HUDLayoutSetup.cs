using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDLayoutSetup : MonoBehaviour
{
    [Header("Assign Elements")]
    public RectTransform healthBarRect;
    public RectTransform chaosBarRect;
    public RectTransform goldTextRect;
    
    [Header("Layout Settings")]
    public float padding = 20f;
    public float healthBarWidth = 300f;
    public float healthBarHeight = 30f;
    
    public float chaosBarWidth = 400f;
    public float chaosBarHeight = 15f;

    [ContextMenu("Apply Cyber Layout")]
    public void ApplyLayout()
    {
        if (healthBarRect != null)
        {
            healthBarRect.anchorMin = new Vector2(0, 0);
            healthBarRect.anchorMax = new Vector2(0, 0);
            healthBarRect.pivot = new Vector2(0, 0);
            
            healthBarRect.anchoredPosition = new Vector2(padding, padding);
            healthBarRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            
            healthBarRect.localRotation = Quaternion.identity; 
            Debug.Log("Health Layout Applied.");
        }
        if (chaosBarRect != null)
        {
            chaosBarRect.anchorMin = new Vector2(0.5f, 1);
            chaosBarRect.anchorMax = new Vector2(0.5f, 1);
            chaosBarRect.pivot = new Vector2(0.5f, 1);
            
            chaosBarRect.anchoredPosition = new Vector2(0, -padding);
            chaosBarRect.sizeDelta = new Vector2(chaosBarWidth, chaosBarHeight);
            
            Debug.Log("Chaos Layout Applied.");
        }

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
            
            Debug.Log("Gold Layout Applied.");
        }
    }
}
