using UnityEngine;
using UnityEngine.UI;

public class GoldGlitchEffect : MonoBehaviour
{
    [Header("Settings")]
    public RawImage glitchImage;
    [Range(0, 500)] public int goldForMaxGlitch = 500;
    
    [Header("Noise Settings")]
    public int textureWidth = 256;
    public int textureHeight = 256;
    [Range(0f, 1f)] public float maxAlpha = 0.8f;

    private Texture2D _noiseTexture;
    private Color32[] _pixels;
    
    void Start()
    {
        _noiseTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        _noiseTexture.filterMode = FilterMode.Point;
        _pixels = new Color32[textureWidth * textureHeight];
        
        if (glitchImage != null)
        {
            glitchImage.texture = _noiseTexture;
            glitchImage.color = new Color(1, 1, 1, 0);
        }
    }

    void Update()
    {
        if (glitchImage == null) return;

        int currentGold = 0;
        var players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                currentGold = p.Gold.Value;
                break;
            }
        }

        float intensity = Mathf.Clamp01((float)currentGold / goldForMaxGlitch);

        float targetAlpha = intensity * maxAlpha;
        
        if (intensity > 0)
        {
            targetAlpha *= Random.Range(0.8f, 1.2f);
            UpdateNoise();
        }
        
        glitchImage.color = new Color(1, 1, 1, targetAlpha);
    }

    void UpdateNoise()
    {
        int y = 0;
        while (y < textureHeight)
        {
            int stripHeight = Random.Range(1, 10);
            Color32 stripColor;

            if (Random.value > 0.8f)
            {
                float r = Random.value > 0.5f ? 255 : 0;
                float g = Random.value > 0.5f ? 255 : 0;
                float b = Random.value > 0.5f ? 255 : 0;
                stripColor = new Color32((byte)r, (byte)g, (byte)b, (byte)Random.Range(100, 255));
            }
            else
            {
                stripColor = new Color32(0, 0, 0, 0);
            }

            for (int h = 0; h < stripHeight && y < textureHeight; h++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    _pixels[y * textureWidth + x] = stripColor;
                }
                y++;
            }
        }
        
        _noiseTexture.SetPixels32(_pixels);
        _noiseTexture.Apply();
        
        if (Random.value < 0.3f)
        {
            Rect uvRect = glitchImage.uvRect;
            uvRect.x = Random.Range(-0.1f, 0.1f);
            uvRect.y = Random.Range(-0.05f, 0.05f);
            uvRect.width = Random.Range(0.9f, 1.1f);
            glitchImage.uvRect = uvRect;
        }
        else
        {
            glitchImage.uvRect = new Rect(0, 0, 1, 1);
        }
    }
}
