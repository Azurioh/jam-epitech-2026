using System.Collections;
using UnityEngine;

public class HulkEffect : MonoBehaviour
{
    [Header("Hulk Settings")]
    [SerializeField] private float sizeMultiplier = 2f;
    [SerializeField] private float healthMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private Color hulkColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    [Header("Animation")]
    [SerializeField] private float growDuration = 0.5f;
    [SerializeField] private float shrinkDuration = 0.4f;

    [Header("Particles")]
    [SerializeField] private bool useParticles = true;

    private Vector3 normalScale;
    private float normalMaxHealth;
    private float normalDamage;

    private Health healthComponent;
    private Weapon weaponComponent;
    private ParticleSystem particles;

    private bool isHulkMode = false;
    private Coroutine currentTransformCoroutine;

    private Material[] originalMaterials;
    private Renderer[] renderers;

    void Start()
    {
        normalScale = transform.localScale;

        healthComponent = GetComponent<Health>();
        weaponComponent = GetComponentInChildren<Weapon>();

        if (healthComponent != null)
        {
            normalMaxHealth = healthComponent.maxHealth;
        }

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                normalDamage = (float)damageField.GetValue(weaponComponent);
            }
        }

        renderers = GetComponentsInChildren<Renderer>();

        if (useParticles)
        {
            CreateParticles();
        }
    }

    void CreateParticles()
    {
        GameObject particlesObject = new GameObject("HulkParticles");
        particlesObject.transform.SetParent(transform);
        particlesObject.transform.localPosition = Vector3.zero;

        particles = particlesObject.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(hulkColor, new Color(0.5f, 1f, 0.3f));
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f;

        var emission = particles.emission;
        emission.rateOverTime = 30f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;
        shape.radiusThickness = 0.8f;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 1f, 0.2f), 0.0f),
                new GradientColorKey(new Color(0.8f, 1f, 0.3f), 0.3f),
                new GradientColorKey(hulkColor, 0.6f),
                new GradientColorKey(new Color(0.1f, 0.4f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var rotationOverLifetime = particles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null || particleShader.name == "Hidden/InternalErrorShader")
        {
            particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }
        if (particleShader == null || particleShader.name == "Hidden/InternalErrorShader")
        {
            particleShader = Shader.Find("Sprites/Default");
        }

        Material particleMaterial = new Material(particleShader);
        particleMaterial.SetColor("_Color", hulkColor);
        if (particleMaterial.HasProperty("_BaseColor"))
        {
            particleMaterial.SetColor("_BaseColor", hulkColor);
        }
        particleMaterial.renderQueue = 3000;

        renderer.material = particleMaterial;


        particles.Stop();
    }

    public void ActivateHulkMode()
    {
        if (isHulkMode) return;

        isHulkMode = true;

        if (currentTransformCoroutine != null)
            StopCoroutine(currentTransformCoroutine);

        currentTransformCoroutine = StartCoroutine(TransformToHulk());
    }

    public void DeactivateHulkMode()
    {
        if (!isHulkMode) return;

        isHulkMode = false;

        if (currentTransformCoroutine != null)
            StopCoroutine(currentTransformCoroutine);

        currentTransformCoroutine = StartCoroutine(TransformToNormal());
    }

    public void ToggleHulkMode()
    {
        if (isHulkMode)
            DeactivateHulkMode();
        else
            ActivateHulkMode();
    }

    public void ActivateHulkModeForDuration(float duration)
    {
        StartCoroutine(HulkModeWithDuration(duration));
    }

    IEnumerator HulkModeWithDuration(float duration)
    {
        ActivateHulkMode();
        yield return new WaitForSeconds(duration);
        DeactivateHulkMode();
    }

    IEnumerator TransformToHulk()
    {
        if (particles != null)
            particles.Play();

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = normalScale * sizeMultiplier;

        float startHealth = healthComponent != null ? healthComponent.maxHealth : 0f;
        float targetHealth = normalMaxHealth * healthMultiplier;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            float eased = EaseOutBack(t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            if (healthComponent != null)
            {
                healthComponent.maxHealth = Mathf.Lerp(startHealth, targetHealth, eased);

                var currentHealthField = typeof(Health).GetField("_currentHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentHealthField != null)
                {
                    var networkVar = currentHealthField.GetValue(healthComponent);
                    var valueProperty = networkVar.GetType().GetProperty("Value");
                    float currentHealth = (float)valueProperty.GetValue(networkVar);
                    float healthRatio = startHealth > 0 ? currentHealth / startHealth : 1f;
                    valueProperty.SetValue(networkVar, targetHealth * healthRatio);
                }
            }

            if (weaponComponent != null)
            {
                var damageField = typeof(Weapon).GetField("damage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (damageField != null)
                {
                    float targetDamage = normalDamage * damageMultiplier;
                    damageField.SetValue(weaponComponent, Mathf.Lerp(normalDamage, targetDamage, eased));
                }
            }

            ChangeMaterialColor(Color.Lerp(Color.white, hulkColor, eased));

            yield return null;
        }

        transform.localScale = targetScale;
        if (healthComponent != null)
            healthComponent.maxHealth = targetHealth;
        ChangeMaterialColor(hulkColor);

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(weaponComponent, normalDamage * damageMultiplier);
            }
        }
    }

    IEnumerator TransformToNormal()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = normalScale;

        float startHealth = healthComponent != null ? healthComponent.maxHealth : 0f;
        float targetHealth = normalMaxHealth;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            float eased = EaseInBack(t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            if (healthComponent != null)
            {
                healthComponent.maxHealth = Mathf.Lerp(startHealth, targetHealth, eased);

                var currentHealthField = typeof(Health).GetField("_currentHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentHealthField != null)
                {
                    var networkVar = currentHealthField.GetValue(healthComponent);
                    var valueProperty = networkVar.GetType().GetProperty("Value");
                    float currentHealth = (float)valueProperty.GetValue(networkVar);
                    float newMaxHealth = Mathf.Lerp(startHealth, targetHealth, eased);
                    if (currentHealth > newMaxHealth)
                    {
                        valueProperty.SetValue(networkVar, newMaxHealth);
                    }
                }
            }

            if (weaponComponent != null)
            {
                var damageField = typeof(Weapon).GetField("damage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (damageField != null)
                {
                    float startDamage = normalDamage * damageMultiplier;
                    damageField.SetValue(weaponComponent, Mathf.Lerp(startDamage, normalDamage, eased));
                }
            }

            ChangeMaterialColor(Color.Lerp(hulkColor, Color.white, eased));

            yield return null;
        }

        transform.localScale = targetScale;
        if (healthComponent != null)
            healthComponent.maxHealth = targetHealth;
        ChangeMaterialColor(Color.white);

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(weaponComponent, normalDamage);
            }
        }

        if (particles != null)
            particles.Stop();
    }

    void ChangeMaterialColor(Color color)
    {
        if (renderers == null) return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
        }
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
}
