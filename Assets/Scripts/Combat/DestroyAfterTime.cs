using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    void Start()
    {
        DamageOnContact dmg = GetComponent<DamageOnContact>();
        if (dmg != null) dmg.EnableHitbox();
        Destroy(gameObject, lifetime);
    }
}
