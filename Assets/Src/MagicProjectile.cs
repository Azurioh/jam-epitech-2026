using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 10;
    public float lifetime = 3f;
    public LayerMask enemyLayer;

    public ParticleSystem hitFX;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Debug.Log("Projectile a touché l'ennemi : " + other.name);
            if (hitFX != null)
            {
                Instantiate(hitFX, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
