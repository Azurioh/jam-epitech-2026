using Unity.Netcode;
using UnityEngine;

public class TowerDefense : NetworkBehaviour // Note qu'on hérite de NetworkBehaviour, pas MonoBehaviour
{
    [SerializeField] private float range = 10f;
    [SerializeField] private float fireRate = 1f;
    private float fireCooldown = 0f;

    // Seul le serveur exécute Update. Les clients ne font que regarder.
    void Update()
    {
        // 1. Sécurité : Si je ne suis pas le serveur, je ne réfléchis pas.
        if (!IsServer) return;

        // 2. Gestion du temps de tir
        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0) return;

        // 3. Chercher l'ennemi le plus proche (Logique très simple pour commencer)
        GameObject target = FindClosestEnemy();

        if (target != null)
        {
            // Tirer (Pour l'instant on affiche juste un log)
            Debug.Log("Pan ! Je tire sur " + target.name);
            fireCooldown = fireRate;

            // Pour faire tourner la tourelle vers l'ennemi
            transform.LookAt(target.transform);
        }
    }

    GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float distance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float curDist = Vector3.Distance(transform.position, enemy.transform.position);
            if (curDist < distance && curDist <= range)
            {
                closest = enemy;
                distance = curDist;
            }
        }
        return closest;
    }
}
