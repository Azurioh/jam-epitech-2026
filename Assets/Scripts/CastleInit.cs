using Unity.Netcode;
using UnityEngine;

public class CastleInit : NetworkBehaviour
{
    public NetworkVariable<int> health = new NetworkVariable<int>(5000);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(5000);
}
