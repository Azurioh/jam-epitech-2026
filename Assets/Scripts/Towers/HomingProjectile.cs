using Unity.Netcode;
using UnityEngine;

public class HomingProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    private Transform target;

    // Fonction appelée par la tourelle juste après le Spawn pour donner la cible
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        // IMPORTANT : Seul le serveur calcule la trajectoire et les collisions
        if (!IsServer) return;

        // Si la cible est morte ou a disparu pendant le trajet, on détruit la balle
        if (target == null)
        {
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        // Mouvement autoguidé vers la cible
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Optionnel : Orienter le projectile vers la cible (visuel)
        transform.LookAt(target);
    }

    // Gestion de la collision
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // On vérifie qu'on a touché un ennemi
        if (other.CompareTag("Enemy"))
        {
            // TODO: Ici tu appelleras plus tard le script de vie de l'ennemi
            // ex: other.GetComponent<EnemyHealth>().TakeDamage(damage);
            other.GetComponent<Health>().TakeDamage(damage);

            Debug.Log("Touché ! " + other.name + " prend " + damage + " dégâts.");

            // On détruit le projectile proprement sur le réseau
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
