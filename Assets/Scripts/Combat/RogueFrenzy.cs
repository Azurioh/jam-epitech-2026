using Unity.Netcode;
using UnityEngine;

public class RogueFrenzy : NetworkBehaviour, IAbility
{
    [Header("Frenzy Settings")]
    [SerializeField] private float cooldown = 15f;
    [SerializeField] private float attackLockDuration = 0.3f;
    [SerializeField] private float frenzyDuration = 10f;
    [SerializeField] private float attackCooldownMultiplier = 0.3f; // 0.3x = attaques 3x plus rapides

    private float lastUseTime = -999f;
    private bool isFrenzyActive = false;
    private PlayerController playerController;
    private float normalAttackCooldown;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController != null)
        {
            var cooldownField = typeof(PlayerController).GetField("attackCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cooldownField != null)
            {
                normalAttackCooldown = (float)cooldownField.GetValue(playerController);
            }
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        if (isFrenzyActive)
        {
            return;
        }

        lastUseTime = Time.time;

        StartCoroutine(FrenzyMode());

        if (IsOwner)
        {
            ActivateFrenzyServerRpc();
        }
    }

    System.Collections.IEnumerator FrenzyMode()
    {
        isFrenzyActive = true;

        if (playerController != null)
        {
            var cooldownField = typeof(PlayerController).GetField("attackCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cooldownField != null)
            {
                float boostedCooldown = normalAttackCooldown * attackCooldownMultiplier;
                cooldownField.SetValue(playerController, boostedCooldown);
            }
        }

        CreateFrenzyParticles();

        yield return new WaitForSeconds(frenzyDuration);

        if (playerController != null)
        {
            var cooldownField = typeof(PlayerController).GetField("attackCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cooldownField != null)
            {
                cooldownField.SetValue(playerController, normalAttackCooldown);
            }
        }

        if (frenzyParticles != null)
        {
            frenzyParticles.Stop();
            Destroy(frenzyParticles.gameObject, 2f);
        }

        isFrenzyActive = false;
    }

    private ParticleSystem frenzyParticles;

    void CreateFrenzyParticles()
    {
        GameObject particlesObject = new GameObject("FrenzyParticles");
        particlesObject.transform.SetParent(transform);
        particlesObject.transform.localPosition = Vector3.zero;

        frenzyParticles = particlesObject.AddComponent<ParticleSystem>();

        var main = frenzyParticles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0f), new Color(1f, 0.5f, 0f)); // Jaune-orange
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = frenzyParticles.emission;
        emission.rateOverTime = 80f;

        var shape = frenzyParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        var velocityOverLifetime = frenzyParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(-2f, 2f);

        var colorOverLifetime = frenzyParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = frenzyParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

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
        particleMaterial.SetColor("_Color", new Color(1f, 0.7f, 0f));
        if (particleMaterial.HasProperty("_BaseColor"))
        {
            particleMaterial.SetColor("_BaseColor", new Color(1f, 0.7f, 0f));
        }
        particleMaterial.renderQueue = 3000;

        renderer.material = particleMaterial;

        frenzyParticles.Play();
    }

    [ServerRpc]
    private void ActivateFrenzyServerRpc()
    {
        ActivateFrenzyClientRpc();
    }

    [ClientRpc]
    private void ActivateFrenzyClientRpc()
    {
        if (!IsOwner)
        {
            StartCoroutine(FrenzyMode());
        }
    }
}
