using Unity.Netcode;
using UnityEngine;

public class BarbarianRage : NetworkBehaviour, IAbility
{
    [Header("Rage Settings")]
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float attackLockDuration = 0.3f;
    [SerializeField] private float rageDuration = 4f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private Color rageColor = new Color(0.6f, 0.1f, 0.1f, 1f);

    [Header("Beer Bottle")]
    [Tooltip("Drag & drop le prefab de bière qui tombera sur le Barbarian")]
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private float beerSpawnHeight = 5f;

    private float lastUseTime = -999f;
    private bool isRageActive = false;
    private Weapon weaponComponent;
    private float normalDamage;
    private ParticleSystem rageParticles;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        weaponComponent = GetComponentInChildren<Weapon>();

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                normalDamage = (float)damageField.GetValue(weaponComponent);
            }
        }

        CreateParticles();
    }

    void CreateParticles()
    {
        GameObject particlesObject = new GameObject("RageParticles");
        particlesObject.transform.SetParent(transform);
        particlesObject.transform.localPosition = Vector3.zero;

        rageParticles = particlesObject.AddComponent<ParticleSystem>();

        var main = rageParticles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(rageColor, new Color(0.8f, 0.2f, 0.2f));
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f;

        var emission = rageParticles.emission;
        emission.rateOverTime = 30f;

        var shape = rageParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;
        shape.radiusThickness = 0.8f;

        var sizeOverLifetime = rageParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = rageParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.8f, 0.2f, 0.2f), 0.0f),
                new GradientColorKey(rageColor, 0.5f),
                new GradientColorKey(new Color(0.4f, 0.05f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var rotationOverLifetime = rageParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

        var velocityOverLifetime = rageParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var noise = rageParticles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;

        var renderer = rageParticles.GetComponent<ParticleSystemRenderer>();
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
        particleMaterial.SetColor("_Color", rageColor);
        if (particleMaterial.HasProperty("_BaseColor"))
        {
            particleMaterial.SetColor("_BaseColor", rageColor);
        }
        particleMaterial.renderQueue = 3000;

        renderer.material = particleMaterial;


        rageParticles.Stop();
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        if (isRageActive)
        {
            return;
        }

        lastUseTime = Time.time;

        SpawnBeer();

        if (IsOwner)
        {
            ActivateRageServerRpc();
        }
    }

    void SpawnBeer()
    {
        if (beerPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * beerSpawnHeight;
            GameObject beer = Instantiate(beerPrefab, spawnPos, Quaternion.identity);

            BeerBottle beerScript = beer.GetComponent<BeerBottle>();
            if (beerScript != null)
            {
                beerScript.Initialize(this);
            }
        }
        else
        {
            StartRageFromBeer();
        }
    }

    public void StartRageFromBeer()
    {
        if (!isRageActive)
        {
            StartCoroutine(RageMode());
        }
    }

    System.Collections.IEnumerator RageMode()
    {
        isRageActive = true;

        if (rageParticles != null)
        {
            rageParticles.Play();
        }

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                float boostedDamage = normalDamage * damageMultiplier;
                damageField.SetValue(weaponComponent, boostedDamage);
            }
        }


        yield return new WaitForSeconds(rageDuration);


        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(weaponComponent, normalDamage);
            }
        }

        if (rageParticles != null)
        {
            rageParticles.Stop();
        }

        isRageActive = false;
    }

    [ServerRpc]
    private void ActivateRageServerRpc()
    {
        ActivateRageClientRpc();
    }

    [ClientRpc]
    private void ActivateRageClientRpc()
    {
        if (!IsOwner)
        {
            SpawnBeer();
        }
    }
}
