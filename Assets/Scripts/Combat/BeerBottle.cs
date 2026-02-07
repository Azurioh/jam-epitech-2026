using UnityEngine;
using System.Collections;

public class BeerBottle : MonoBehaviour
{
    [Header("Fall Settings")]
    [SerializeField] private float fallSpeed = 8f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float lifetime = 3f;

    [Header("Effects")]
    [SerializeField] private Color beerColor = new Color(1f, 0.8f, 0.2f); // Doré

    private bool hasExploded = false;
    private BarbarianRage targetRage;

    public void Initialize(BarbarianRage rage)
    {
        targetRage = rage;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (other.GetComponent<BarbarianRage>() != null ||
            other.GetComponentInParent<BarbarianRage>() != null)
        {
            hasExploded = true;
            ExplodeAndActivateRage();
        }
    }

    void ExplodeAndActivateRage()
    {
        CreateBeerExplosion();

        if (targetRage != null)
        {
            targetRage.StartRageFromBeer();
        }

        Destroy(gameObject);
    }

    void CreateBeerExplosion()
    {
        GameObject explosionObject = new GameObject("BeerExplosion");
        explosionObject.transform.position = transform.position;

        ParticleSystem ps = explosionObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.maxParticles = 100;
        main.duration = 1f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 50, 100)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0.3f),
                new GradientColorKey(new Color(0.9f, 0.6f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0.0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.3f, 1.5f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null || shader.name == "Hidden/InternalErrorShader")
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material mat = new Material(shader);
        mat.color = beerColor;
        renderer.material = mat;

        ps.Play();

        Destroy(explosionObject, 2f);
    }
}
