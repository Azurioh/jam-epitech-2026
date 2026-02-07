using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerStats : NetworkBehaviour
{
    // Variables synchronisées sur le réseau
    // NetworkVariableWritePermission.Server : seul le serveur peut modifier (sécurité)
    // IMPORTANT : Les NetworkVariable doivent être instanciées à la déclaration, pas dans OnNetworkSpawn
    public NetworkVariable<int> Health = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Gold = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Initialiser les valeurs depuis les Visual Scripting Variables (côté serveur uniquement)
        if (IsServer)
        {
            Health.Value = Variables.Object(this.gameObject).Get<int>("HEALTH");
            Gold.Value = Variables.Object(this.gameObject).Get<int>("GOLD");
        }

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
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamageServerRpc(10);
            Debug.Log("[DEBUG] Raccourci H: -10 HP");
        }

        // G = Gagner 50 Gold (pour tester le glitch effect)
        if (Keyboard.current.gKey.wasPressedThisFrame)
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpendGoldServerRpc(int amount)
    {
        SpendGold(amount);
    }

    /// <summary>
    /// Déduit de l'or côté serveur. Peut être appelé directement depuis un autre script serveur.
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (Gold.Value >= amount)
        {
            Gold.Value -= amount;
            Debug.Log($"Player {OwnerClientId} spent {amount} gold. Gold: {Gold.Value}");
            return true;
        }
        return false;
    }
}
