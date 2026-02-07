using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ce script doit être placé dans la scène de jeu (NathanGame).
/// Il spawn le personnage choisi dans la scène de sélection.
/// </summary>
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
            // Spawn les joueurs quand ils se connectent
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
        // Demande au client son index de personnage puis spawn
        SpawnPlayerForClient(clientId);
    }
    
    private void SpawnPlayerForClient(ulong clientId)
    {
        // Récupère l'index du personnage sélectionné
        int characterIndex = CharacterSelector.SelectedCharacterIndex;
        
        // Vérifie que l'index est valide
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"Invalid character index: {characterIndex}. Using default (0).");
            characterIndex = 0;
        }
        
        // Position de spawn
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        spawnPos += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)); // Offset aléatoire
        
        // Spawn le personnage
        GameObject player = Instantiate(characterPrefabs[characterIndex], spawnPos, Quaternion.identity);
        
        // Spawn sur le réseau et donne le contrôle au client
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
