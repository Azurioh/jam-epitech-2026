using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Character Prefabs")]
    [Tooltip("Doit être dans le MÊME ORDRE que dans CharacterSelector")]
    public GameObject[] characterPrefabs;

    [Header("Spawn Settings")]
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            // Le host : on a directement son index local
            SpawnPlayerForClient(clientId, CharacterSelector.SelectedCharacterIndex);
        }
        // Les clients enverront leur index via RequestSpawnServerRpc
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnServerRpc(int characterIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Vérifie que ce client n'a pas déjà un player object
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) return;

        SpawnPlayerForClient(clientId, characterIndex);
    }

    private void SpawnPlayerForClient(ulong clientId, int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"Invalid character index: {characterIndex}. Using default (0).");
            characterIndex = 0;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        spawnPos += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

        GameObject player = Instantiate(characterPrefabs[characterIndex], spawnPos, Quaternion.identity);

        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
            Debug.Log($"Spawned character {characterIndex} for client {clientId}");
        }
        else
        {
            Debug.LogError("Character prefab is missing NetworkObject component!");
            Destroy(player);
        }
    }
}
