using UnityEngine;

public class AOEZone : MonoBehaviour
{
    [Header("AOE Settings")]
    public float radius = 5f;
    public float duration = 4f;
    public float damagePerSecond = 20f;
    public LayerMask enemyLayer;

    [Header("Visual")]
    public Color zoneColor = new Color(0.3f, 0.7f, 1f, 0.8f);
    public Color edgeColor = new Color(0.7f, 0.9f, 1f, 1f);

    [Header("Animation")]
    public float growDuration = 0.3f;
    public bool destroyOnEnd = true;

    private MeshRenderer meshRenderer;
    private Material material;
    private ParticleSystem fireParticles;
    private float spawnTime;
    private float currentScale;
    private bool isShrinking;

    void Start()
    {
        spawnTime = Time.time;
        CreateVisual();
        CreateParticles();
    }

    void CreateVisual()
    {
        // Crée un quad (plan) comme enfant pour l'affichage
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "AOE_Visual";
        visual.transform.SetParent(transform);
        visual.transform.localPosition = new Vector3(0, 0.05f, 0); // Légèrement au-dessus du sol
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // À plat sur le sol
        visual.transform.localScale = Vector3.zero;

        // Supprime le collider du quad
        Destroy(visual.GetComponent<Collider>());

        meshRenderer = visual.GetComponent<MeshRenderer>();

        // Charge le shader et crée le material
        Shader shader = Shader.Find("Custom/AOEZone");
        if (shader == null)
        {
            Debug.LogError("AOEZone: Shader 'Custom/AOEZone' non trouvé !");
            return;
        }

        material = new Material(shader);
        material.SetColor("_Color", zoneColor);
        material.SetColor("_EdgeColor", edgeColor);
        material.SetFloat("_Radius", 1f / 1.3f);
        meshRenderer.material = material;
    }

    void CreateParticles()
    {
        // --- Braises / étincelles qui montent ---
        GameObject embersGO = new GameObject("AOE_Embers");
        embersGO.transform.SetParent(transform);
        embersGO.transform.localPosition = Vector3.zero;

        fireParticles = embersGO.AddComponent<ParticleSystem>();
        var main = fireParticles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.9f, 1f, 1f),
            new Color(0.4f, 0.6f, 1f, 1f)
        );
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Les particules montent

        var emission = fireParticles.emission;
        emission.rateOverTime = 40f;

        // Shape: cercle à plat sur le sol (rotation -90 en X pour passer de XY à XZ)
        var shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        // Les particules diminuent en taille
        var sizeOverLifetime = fireParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Fade out en alpha
        var colorOverLifetime = fireParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.7f, 1f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.3f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Renderer: utilise le material par défaut de particules
        var renderer = embersGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", new Color(0.6f, 0.85f, 1f, 1f));

        fireParticles.Play();
    }

    void Update()
    {
        float elapsed = Time.time - spawnTime;

        // Phase de croissance
        if (elapsed < growDuration)
        {
            float t = elapsed / growDuration;
            // Easing out elastic pour un effet "pop"
            t = EaseOutBack(t);
            currentScale = t;
        }
        // Phase active
        else if (elapsed < duration - 0.5f)
        {
            currentScale = 1f;
            DealDamage();
        }
        // Phase de rétrécissement (dernière 0.5s)
        else if (elapsed < duration)
        {
            if (!isShrinking)
                isShrinking = true;

            float shrinkT = (elapsed - (duration - 0.5f)) / 0.5f;
            currentScale = 1f - EaseInBack(shrinkT);
            DealDamage();
        }
        else
        {
            if (destroyOnEnd)
                Destroy(gameObject);
            return;
        }

        // Applique l'échelle au visuel (marge 1.3x pour que le cercle + pulse + bord ne soit pas coupé)
        float size = radius * 2f * 1.3f * Mathf.Max(currentScale, 0f);
        Transform visual = transform.Find("AOE_Visual");
        if (visual != null)
            visual.localScale = new Vector3(size, size, size);

        // Met à jour le rayon des particules
        if (fireParticles != null)
        {
            var shape = fireParticles.shape;
            shape.radius = radius * Mathf.Max(currentScale, 0f);

            var emission = fireParticles.emission;
            emission.rateOverTime = 40f * Mathf.Max(currentScale, 0f);
        }
    }

    void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius * currentScale, enemyLayer);
        foreach (Collider hit in hits)
        {
            // Applique les dégâts - adapte selon ton système de vie
            // Exemple: hit.GetComponent<Health>()?.TakeDamage(damagePerSecond * Time.deltaTime);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
