using Unity.Netcode;
using UnityEngine;

public class MobReward : NetworkBehaviour
{
    public int goldReward = 50;

    private Health healthComponent;
    private bool hasRewarded = false;

    void Start()
    {
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            Debug.LogError("MobReward: Aucun composant Health trouvé sur " + gameObject.name);
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (healthComponent != null && !hasRewarded && !healthComponent.IsAlive)
        {
            GiveRewardToAllPlayers();
            hasRewarded = true;
        }
    }

    private void GiveRewardToAllPlayers()
    {
        if (!IsServer) return;

        PlayerStats[] allPlayers = FindObjectsOfType<PlayerStats>();

        foreach (PlayerStats player in allPlayers)
        {
            if (player.IsSpawned)
            {
                player.AddGoldServerRpc(goldReward);
            }
        }

        Debug.Log($"Mob {gameObject.name} tué ! {goldReward} gold distribué à {allPlayers.Length} joueur(s)");
    }
}
