using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // Nécessaire pour Mouse.current

public class PlayerBuildSystem : NetworkBehaviour
{
    [Header("Références")]
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Camera playerCamera;

    [Header("Réglages de Construction")]
    [SerializeField] private float turretRadius = 0.8f; // Rayon de place qu'elle occupe
    [SerializeField] private LayerMask groundLayer;     // Layer "Ground"
    [SerializeField] private LayerMask turretLayer;     // Layer "Turret"

    // Cette partie est PARFAITE, on la garde telle quelle
    public override void OnNetworkSpawn()
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = IsOwner;
            var audioListener = playerCamera.GetComponent<AudioListener>();
            if (audioListener != null) audioListener.enabled = IsOwner;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // On vérifie le clic gauche via le New Input System
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RequestBuild();
        }
    }

    void RequestBuild()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        // MODIFICATION 1 : On ajoute "groundLayer" dans le Raycast
        // Cela empêche de cliquer sur le ciel, sur un autre joueur ou sur une tourelle existante.
        // On ne peut cliquer QUE sur le sol.
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // On envoie la position au serveur (+ un petit décalage vers le haut pour pas être dans le sol)
            BuildTowerServerRpc(hit.point);
        }
    }

    [ServerRpc]
    void BuildTowerServerRpc(Vector3 position)
    {
        // MODIFICATION 2 : La vérification de collision (Overlap)
        // Le serveur vérifie : "Y a-t-il déjà un objet du layer 'Turret' ici ?"
        bool isBlocked = Physics.CheckSphere(position, turretRadius, turretLayer);

        if (!isBlocked)
        {
            // C'est libre, on construit !
            // J'ajoute Quaternion.identity pour la rotation par défaut
            GameObject newTower = Instantiate(towerPrefab, position, Quaternion.identity);
            newTower.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.Log("Construction impossible : Zone occupée !");
        }
    }
}
