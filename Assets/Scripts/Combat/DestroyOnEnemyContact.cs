using Unity.Netcode;
using UnityEngine;

public class DestroyOnEnemyContact : MonoBehaviour
{
    [SerializeField] private LayerMask destroyOnContactLayer;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & destroyOnContactLayer) != 0)
            DestroyObject();
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & destroyOnContactLayer) != 0)
            DestroyObject();
    }

    private void DestroyObject()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
                netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
