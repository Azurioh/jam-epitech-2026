using UnityEngine;

public class DestroyOnEnemyContact : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
            Destroy(gameObject);
    }
}
