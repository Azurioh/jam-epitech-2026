using Unity.Netcode;
using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    void Start()
    {
        DamageOnContact dmg = GetComponent<DamageOnContact>();
        if (dmg != null) dmg.EnableHitbox();
        Invoke(nameof(DestroyObject), lifetime);
    }

    private void DestroyObject()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
                netObj.Despawn();
            // Client : ne rien faire, le serveur gère
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
