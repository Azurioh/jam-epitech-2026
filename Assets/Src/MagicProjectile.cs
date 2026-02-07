using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 3f;


    void Start()
    {
        Destroy(gameObject, lifetime);
        DamageOnContact dmg = GetComponent<DamageOnContact>();
        if (dmg != null) dmg.EnableHitbox();
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

}
