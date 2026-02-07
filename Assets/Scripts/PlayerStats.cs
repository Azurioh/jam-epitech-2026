using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : NetworkBehaviour
{
    // Variables synchronisées sur le réseau
    // NetworkVariableWritePermission.Server : seul le serveur peut modifier (sécurité)
    public NetworkVariable<int> Health;
    public NetworkVariable<int> Gold;

    public override void OnNetworkSpawn()
    {
        Health = new NetworkVariable<int>(Variables.Object(this.gameObject).Get<int>("HEALTH"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        Gold = new NetworkVariable<int>(Variables.Object(this.gameObject).Get<int>("GOLD"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // Si c'est MON joueur local, je m'abonne aux changements pour mettre à jour l'UI
        if (IsOwner)
        {
            // Initial UI Update
            UpdateHUD();

            // S'abonner aux changements de valeurs
            Health.OnValueChanged += (oldValue, newValue) => UpdateHUD();
            Gold.OnValueChanged += (oldValue, newValue) => UpdateHUD();
        }
    }

    private void UpdateHUD()
    {
        // On vérifie que le HUDController existe (Singleton)
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateHealth(Health.Value);
            HUDController.Instance.UpdateGold(Gold.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Health.OnValueChanged -= (oldValue, newValue) => UpdateHUD();
            Gold.OnValueChanged -= (oldValue, newValue) => UpdateHUD();
        }
    }

    // --- RACCOURCIS DE TEST (DEV ONLY) ---
    void Update()
    {
        // Seulement pour le joueur local
        if (!IsOwner) return;

        // H = Perdre 10 HP (pour tester la healthbar)
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamageServerRpc(10);
            Debug.Log("[DEBUG] Raccourci H: -10 HP");
        }

        // G = Gagner 50 Gold (pour tester le glitch effect)
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddGoldServerRpc(50);
            Debug.Log("[DEBUG] Raccourci G: +50 Gold");
        }
    }

    // --- FONCTIONS LOGIQUES (Côté Serveur uniquement pour la triche) ---

    // Pour tester, on peut appeler ça depuis un input ou une collision
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] // N'importe qui peut demander à prendre des dégâts (collisions etc)
    public void TakeDamageServerRpc(int damage)
    {
        if (Health.Value > 0)
        {
            Health.Value -= damage;
            if (Health.Value < 0) Health.Value = 0;
            Debug.Log($"Player {OwnerClientId} took {damage} damage. HP: {Health.Value}");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddGoldServerRpc(int amount)
    {
        Gold.Value += amount;
        Debug.Log($"Player {OwnerClientId} received {amount} gold. Gold: {Gold.Value}");
    }
}
