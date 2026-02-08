using System.Collections;
using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float shieldRadius = 2f;
    [SerializeField] private Vector3 shieldOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.5f, 1f, 0.3f);
    [SerializeField] private Color particleColor = new Color(0.5f, 0.7f, 1f, 1f);

    [Header("Custom Shader (Optional)")]
    [Tooltip("Drag ton shader TMP ici depuis le Project")]
    [SerializeField] private Shader customShader;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Rotation")]
    [SerializeField] private bool rotateShield = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 30f, 0);

    private GameObject shieldObject;
    private Renderer shieldRenderer;
    private Material shieldMaterial;
    private ParticleSystem shieldParticles;
    private float baseRadius;
    private bool isActive = false;
    private float currentScale = 0f;

    void Start()
    {
        baseRadius = shieldRadius;
        CreateShield();
        CreateParticles();
        shieldObject.SetActive(false);
    }

    void CreateShield()
    {
        shieldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObject.name = "ShieldEffect";
        shieldObject.transform.SetParent(transform);
        shieldObject.transform.localPosition = shieldOffset;
        shieldObject.transform.localScale = Vector3.zero;

        Destroy(shieldObject.GetComponent<Collider>());

        shieldRenderer = shieldObject.GetComponent<Renderer>();

        Shader shader = customShader;

        if (shader == null)
            shader = Shader.Find("TextMeshPro/SRP/TMP_SDF-URP Unlit");
        if (shader != null)
        {
            shieldMaterial = new Material(shader);
        }
        else
        {
            shieldMaterial = new Material(Shader.Find("Standard"));
            shieldMaterial.SetFloat("_Mode", 3);
            shieldMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shieldMaterial.SetInt("_ZWrite", 0);
            shieldMaterial.DisableKeyword("_ALPHATEST_ON");
            shieldMaterial.EnableKeyword("_ALPHABLEND_ON");
            shieldMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        shieldMaterial.color = shieldColor;
        shieldMaterial.renderQueue = 3000;

        shieldRenderer.material = shieldMaterial;
        shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shieldRenderer.receiveShadows = false;

        Debug.Log($"Shield shader: {shieldMaterial.shader.name}");
    }

    void CreateParticles()
    {
        GameObject particlesGO = new GameObject("Shield_Particles");
        particlesGO.transform.SetParent(shieldObject.transform);
        particlesGO.transform.localPosition = Vector3.zero;

        shieldParticles = particlesGO.AddComponent<ParticleSystem>();
        var main = shieldParticles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(particleColor);
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = shieldParticles.emission;
        emission.rateOverTime = 30f;

        var shape = shieldParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        shape.radiusThickness = 0.1f;

        var sizeOverLifetime = shieldParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = shieldParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.6f, 1f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.4f, 0.9f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = particlesGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", particleColor);

        shieldParticles.Stop();
    }

    void Update()
    {
        if (isActive && shieldObject != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            shieldObject.transform.localScale = Vector3.one * baseRadius * 2f * currentScale * pulse;

            if (rotateShield)
            {
                shieldObject.transform.Rotate(rotationSpeed * Time.deltaTime);
            }

            float intensity = 0.8f + Mathf.Sin(Time.time * pulseSpeed * 2f) * 0.2f;
            Color currentColor = shieldColor * intensity;
            currentColor.a = shieldColor.a;
            shieldMaterial.color = currentColor;
        }
    }

    public void ActivateShield()
    {
        if (isActive) return;

        isActive = true;
        shieldObject.SetActive(true);
        if (shieldParticles != null) shieldParticles.Play();
        StartCoroutine(FadeIn());
    }

    public void DeactivateShield()
    {
        if (!isActive) return;

        isActive = false;
        if (shieldParticles != null) shieldParticles.Stop();
        StartCoroutine(FadeOut());
    }

    public void ToggleShield()
    {
        if (isActive)
            DeactivateShield();
        else
            ActivateShield();
    }

    public void ActivateShieldForDuration(float duration)
    {
        ActivateShield();
        Invoke(nameof(DeactivateShield), duration);
    }

    public bool IsShieldActive()
    {
        return isActive;
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            currentScale = EaseOutBack(t);

            float alpha = Mathf.Lerp(0f, shieldColor.a, t);
            Color currentColor = shieldColor;
            currentColor.a = alpha;
            shieldMaterial.color = currentColor;

            yield return null;
        }

        currentScale = 1f;
        shieldMaterial.color = shieldColor;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = shieldColor.a;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            currentScale = 1f - EaseInBack(t);

            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            Color currentColor = shieldColor;
            currentColor.a = alpha;
            shieldMaterial.color = currentColor;

            yield return null;
        }

        shieldObject.SetActive(false);
        currentScale = 0f;
        shieldMaterial.color = shieldColor;
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    public bool IsActive => isActive;

    void OnDestroy()
    {
        if (shieldMaterial != null)
            Destroy(shieldMaterial);
    }
}
