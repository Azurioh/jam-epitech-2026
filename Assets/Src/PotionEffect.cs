using UnityEngine;
using UnityEngine.InputSystem;

public class PotionEffect : MonoBehaviour
{
    public ParticleSystem potionParticles;
    public float duration = 10f;

    private float effectEndTime;
    private bool isActive;

    void Start()
    {
        if (potionParticles != null)
        {
            var main = potionParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    void Update()
    {
        if (isActive && Time.time >= effectEndTime)
        {
            StopEffect();
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartEffect();
        }
    }

    public void StartEffect()
    {
        isActive = true;
        effectEndTime = Time.time + duration;

        if (potionParticles != null)
        {
            potionParticles.Play();
        }
    }

    public void StopEffect()
    {
        isActive = false;

        if (potionParticles != null)
        {
            potionParticles.Stop();
        }
    }
}
