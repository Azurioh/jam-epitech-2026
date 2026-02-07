using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // <--- Indispensable pour le nouveau système

public class TowerSpawner : NetworkBehaviour
{
    [Header("Prévisualisation (Ghost)")]
    public GameObject towerGhostPrefab;
    public Material validMat;
    public Material invalidMat;

    [Header("Construction")]
    public GameObject realTowerPrefab;
    public float towerRadius = 1.0f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    // IMPORTANT : On remet la référence explicite à la caméra du joueur
    [SerializeField] private Camera playerCamera;

    private GameObject currentGhost;
    private Renderer[] ghostRenderers;
    private bool isBuildMode = false;

    // La caméra est maintenant gérée entièrement par PlayerController
    public override void OnNetworkSpawn()
    {
        // Récupérer la caméra du joueur local pour le raycast (si pas déjà assignée dans l'inspecteur)
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleBuildMode();
        }

        if (isBuildMode && currentGhost != null)
        {
            HandlePreview();
        }
    }

    void ToggleBuildMode()
    {
        isBuildMode = !isBuildMode;

        if (isBuildMode)
        {
            currentGhost = Instantiate(towerGhostPrefab);
            ghostRenderers = currentGhost.GetComponentsInChildren<Renderer>();
            foreach (var col in currentGhost.GetComponentsInChildren<Collider>()) col.enabled = false;
        }
        else
        {
            if (currentGhost != null) Destroy(currentGhost);
        }
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

            bool isBlocked = Physics.CheckSphere(targetPos, towerRadius, obstacleLayer);
            UpdateGhostColor(!isBlocked);

            if (Mouse.current.leftButton.wasPressedThisFrame && !isBlocked)
            {
                BuildTowerServerRpc(targetPos);
            }
        }
        else
        {
            // Si on pointe le ciel ou hors du sol, on cache le fantôme
            currentGhost.SetActive(false);
        }
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
    void BuildTowerServerRpc(Vector3 position)
    {
        if (!Physics.CheckSphere(position, towerRadius, obstacleLayer))
        {
            GameObject newTower = Instantiate(realTowerPrefab, position, Quaternion.identity);
            newTower.GetComponent<NetworkObject>().Spawn();
        }
    }
}
