using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SlowZone : MonoBehaviour
{
    [SerializeField] private float slowMultiplier = 0.4f;
    [SerializeField] private LayerMask enemyLayer;

    private Dictionary<NavMeshAgent, float> _originalSpeeds = new Dictionary<NavMeshAgent, float>();

    public void SetParameters(float multiplier, LayerMask layer)
    {
        slowMultiplier = multiplier;
        enemyLayer = layer;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        NavMeshAgent agent = other.GetComponentInParent<NavMeshAgent>();
        if (agent != null && !_originalSpeeds.ContainsKey(agent))
        {
            _originalSpeeds[agent] = agent.speed;
            agent.speed *= slowMultiplier;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        NavMeshAgent agent = other.GetComponentInParent<NavMeshAgent>();
        if (agent != null && _originalSpeeds.ContainsKey(agent))
        {
            agent.speed = _originalSpeeds[agent];
            _originalSpeeds.Remove(agent);
        }
    }

    void OnDestroy()
    {
        // Restore toutes les vitesses quand la zone disparait
        foreach (var kvp in _originalSpeeds)
        {
            if (kvp.Key != null)
                kvp.Key.speed = kvp.Value;
        }
        _originalSpeeds.Clear();
    }
}
