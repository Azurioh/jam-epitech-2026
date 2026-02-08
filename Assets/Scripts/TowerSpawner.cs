using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TowerSpawner : NetworkBehaviour
{
    [Header("Prévisualisation (Ghost)")]
    public Material validMat;
    public Material invalidMat;

    [Header("Construction")]
    public GameObject[] towerPrefabs;
    public float towerRadius = 2.0f;
    public float towerHeightCheck = 1.5f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [SerializeField] private Camera playerCamera;

    [Header("Prix")]
    public int currentTowerPrice = 0;

    private GameObject currentGhost;
    private Renderer[] ghostRenderers;
    private bool isBuildMode = false;
    private int selectedTowerIndex = 0;
    private PlayerStats playerStats;
    private TMP_Text textPriceTower;
    private GameObject uiPriceTower;

    // La caméra est maintenant gérée entièrement par PlayerController
    public override void OnNetworkSpawn()
    {
        // Récupérer la caméra du joueur local pour le raycast (si pas déjà assignée dans l'inspecteur)
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        // Récupérer le PlayerStats du joueur
        playerStats = GetComponent<PlayerStats>();

        // Chercher les éléments UI tant qu'ils sont encore actifs
        FindUIElements();
        if (uiPriceTower != null) uiPriceTower.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner) return;

        // Sélection de tour avec les touches 1, 2, 3...
        HandleTowerSelection();

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBuildMode();
        }

        if (isBuildMode && currentGhost != null)
        {
            HandlePreview();
        }
    }

    void HandleTowerSelection()
    {
        if (towerPrefabs == null || towerPrefabs.Length == 0) return;

        int previousIndex = selectedTowerIndex;

        // Touches 1 à 9 pour sélectionner les tours
        if (Keyboard.current.digit1Key.wasPressedThisFrame && towerPrefabs.Length >= 1)
            selectedTowerIndex = 0;
        else if (Keyboard.current.digit2Key.wasPressedThisFrame && towerPrefabs.Length >= 2)
            selectedTowerIndex = 1;
        else if (Keyboard.current.digit3Key.wasPressedThisFrame && towerPrefabs.Length >= 3)
            selectedTowerIndex = 2;
        else if (Keyboard.current.digit4Key.wasPressedThisFrame && towerPrefabs.Length >= 4)
            selectedTowerIndex = 3;
        else if (Keyboard.current.digit5Key.wasPressedThisFrame && towerPrefabs.Length >= 5)
            selectedTowerIndex = 4;
        else if (Keyboard.current.digit6Key.wasPressedThisFrame && towerPrefabs.Length >= 6)
            selectedTowerIndex = 5;

        // Si on a changé de sélection et qu'on est en mode build, recréer le ghost
        if (previousIndex != selectedTowerIndex && isBuildMode)
        {
            UpdateGhost();
            UpdatePriceDisplay();
        }
    }

    void ToggleBuildMode()
    {
        isBuildMode = !isBuildMode;

        if (isBuildMode)
        {
            UpdateGhost();
            UpdatePriceDisplay();
            if (uiPriceTower != null) uiPriceTower.SetActive(true);
        }
        else
        {
            if (currentGhost != null) Destroy(currentGhost);
            if (uiPriceTower != null) uiPriceTower.SetActive(false);
        }
    }

    void FindUIElements()
    {
        GameObject textObj = GameObject.Find("TextPriceTower");
        if (textObj != null)
            textPriceTower = textObj.GetComponent<TMP_Text>();

        uiPriceTower = GameObject.Find("UIPriceTower");
    }

    void UpdatePriceDisplay()
    {
        currentTowerPrice = GetSelectedTowerPrice();
        if (textPriceTower != null)
            textPriceTower.text = currentTowerPrice.ToString();
    }

    void UpdateGhost()
    {
        // Détruire l'ancien ghost s'il existe
        if (currentGhost != null) Destroy(currentGhost);

        // Créer le nouveau ghost basé sur la tour sélectionnée
        if (towerPrefabs != null && towerPrefabs.Length > selectedTowerIndex && towerPrefabs[selectedTowerIndex] != null)
        {
            currentGhost = CreateGhostFromTower(towerPrefabs[selectedTowerIndex]);
            ghostRenderers = currentGhost.GetComponentsInChildren<Renderer>();
        }
    }

    /// <summary>
    /// Retourne le prefab de la tour actuellement sélectionnée
    /// </summary>
    GameObject GetSelectedTowerPrefab()
    {
        if (towerPrefabs == null || towerPrefabs.Length == 0) return null;
        return towerPrefabs[selectedTowerIndex];
    }

    /// <summary>
    /// Crée un ghost dynamiquement à partir du prefab de la tour.
    /// Le ghost garde le visuel mais sans les comportements (scripts, colliders, network).
    /// </summary>
    GameObject CreateGhostFromTower(GameObject towerPrefab)
    {
        // Instancier une copie du prefab
        GameObject ghost = Instantiate(towerPrefab);
        ghost.name = towerPrefab.name + "_Ghost";

        // Désactiver tous les colliders (pour ne pas bloquer le placement)
        foreach (var col in ghost.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // Supprimer le NetworkObject et autres composants réseau
        var networkObj = ghost.GetComponent<NetworkObject>();
        if (networkObj != null) Destroy(networkObj);

        // Supprimer tous les NetworkBehaviour (TowerDefense, etc.)
        foreach (var netBehaviour in ghost.GetComponentsInChildren<NetworkBehaviour>())
        {
            Destroy(netBehaviour);
        }

        // Supprimer les autres scripts qui pourraient causer des problèmes
        // (on garde seulement Transform et les composants de rendu)
        foreach (var monoBehaviour in ghost.GetComponentsInChildren<MonoBehaviour>())
        {
            // On ne détruit pas ce TowerSpawner si c'est attaché au joueur
            if (monoBehaviour != this)
            {
                Destroy(monoBehaviour);
            }
        }

        // Appliquer le matériau valide par défaut
        var renderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            // Créer un tableau de matériaux de la même taille
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = validMat;
            }
            rend.materials = mats;
        }

        return ghost;
    }

    void HandlePreview()
    {
        // On utilise playerCamera (la tienne) et pas Camera.main (celle des autres potentiellement)
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // POSITION EXACTE (Plus de Grid !)
            Vector3 targetPos = hit.point;

            currentGhost.transform.position = targetPos;
            currentGhost.SetActive(true);

            // Vérifie si la position est bloquée par un obstacle OU une autre tour
            bool isBlocked = IsPositionBlocked(targetPos);
            bool canAfford = CanAffordTower();
            bool canPlace = !isBlocked && canAfford;
            UpdateGhostColor(canPlace);

            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                BuildTowerServerRpc(targetPos, selectedTowerIndex);
            }
        }
        else
        {
            // Si on pointe le ciel ou hors du sol, on cache le fantôme
            currentGhost.SetActive(false);
        }
    }

    /// <summary>
    /// Retourne le prix de la tour actuellement sélectionnée
    /// </summary>
    int GetSelectedTowerPrice()
    {
        GameObject prefab = GetSelectedTowerPrefab();
        if (prefab == null) return int.MaxValue;
        TowerDefense towerDef = prefab.GetComponent<TowerDefense>();
        if (towerDef == null) return 0;
        return towerDef.price;
    }

    /// <summary>
    /// Vérifie si le joueur a assez d'or pour la tour sélectionnée
    /// </summary>
    bool CanAffordTower()
    {
        if (playerStats == null) return false;
        return playerStats.Gold.Value >= GetSelectedTowerPrice();
    }

    /// <summary>
    /// Vérifie si une position est bloquée par un obstacle ou une autre tour
    /// </summary>
    bool IsPositionBlocked(Vector3 position)
    {
        // On décale le point de vérification vers le HAUT pour détecter le centre des tours, pas le sol
        Vector3 checkPosition = position + Vector3.up * towerHeightCheck;

        // 1. Vérifier les obstacles classiques
        if (Physics.CheckSphere(checkPosition, towerRadius, obstacleLayer))
        {
            return true;
        }

        // 2. Vérifier s'il y a une tour (avec le tag "Tower") dans le rayon
        Collider[] colliders = Physics.OverlapSphere(checkPosition, towerRadius);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Tower"))
            {
                return true;
            }
        }

        return false;
    }

    void UpdateGhostColor(bool isValid)
    {
        Material targetMat = isValid ? validMat : invalidMat;
        foreach (var rend in ghostRenderers)
        {
            rend.material = targetMat;
        }
    }

    [ServerRpc]
    void BuildTowerServerRpc(Vector3 position, int towerIndex)
    {
        // Validation de l'index
        if (towerPrefabs == null || towerIndex < 0 || towerIndex >= towerPrefabs.Length) return;
        if (towerPrefabs[towerIndex] == null) return;

        // Vérification côté serveur (position + gold)
        int towerPrice = towerPrefabs[towerIndex].GetComponent<TowerDefense>() != null
            ? towerPrefabs[towerIndex].GetComponent<TowerDefense>().price
            : 0;

        // Récupérer le PlayerStats du joueur qui a fait la requête
        PlayerStats requesterStats = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<PlayerStats>();
        if (requesterStats == null || requesterStats.Gold.Value < towerPrice) return;

        if (!IsPositionBlocked(position))
        {
            // Déduire l'or via PlayerStats (sur son propre NetworkBehaviour)
            if (!requesterStats.SpendGold(towerPrice)) return;

            GameObject newTower = Instantiate(towerPrefabs[towerIndex], position, Quaternion.identity);

            // Assigner le tag "Tower" à la tour (pour la détection de collision future)
            newTower.tag = "Tower";

            newTower.GetComponent<NetworkObject>().Spawn();
        }
    }
}
