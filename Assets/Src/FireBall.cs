using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float initialSpeed = 15f;
    public float damageRadius = 2f;
    public int damage = 30;
    public LayerMask enemyLayer;
    public LayerMask groundLayer;

    public ParticleSystem explosionFX;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * initialSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0 ||
            ((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (explosionFX != null)
            explosionFX.Play();

        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
        }

        Destroy(gameObject, 3f);
    }
}