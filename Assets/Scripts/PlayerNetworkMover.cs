using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkMover : NetworkBehaviour
{
    public float speed = 5f;

    [SerializeField] private PlayerInput playerInput; // Référence au composant Player Input
    // Si tu as un script "PlayerController" local (celui de ton screenshot 1), ajoute une référence aussi :
    // [SerializeField] private PlayerController localController;

    public override void OnNetworkSpawn()
    {
        // Si cet objet appartient à MOI (le joueur local)
        if (IsOwner)
        {
            MoveToRandomPosition();
            if (playerInput != null) playerInput.enabled = true;
        }
        else
        {
            // Si cet objet est un autre joueur (un fantôme/proxy)
            // ON COUPE TOUT pour ne pas que mon clavier le contrôle
            if (playerInput != null) playerInput.enabled = false;

            // IMPORTANT : Désactiver aussi la caméra et l'AudioListener des autres (tu l'as déjà fait dans TowerSpawner, mais c'est bien de vérifier ici aussi)
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;

            var audio = GetComponentInChildren<AudioListener>();
            if (audio != null) audio.enabled = false;

            // Si tu utilises le CharacterController pour la physique, désactive-le aussi sur les proxies pour éviter les conflits avec le NetworkTransform
            var charController = GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;
        }
    }

    void MoveToRandomPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        transform.position = randomPosition;
    }

    void Update()
    {
        if (!IsOwner) return;

        // ... Le reste de ton code de déplacement est correct ...
        Vector3 moveDir = Vector3.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveDir.z = +1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveDir.z = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveDir.x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveDir.x = +1f;
        }

        transform.position += moveDir.normalized * speed * Time.deltaTime;
        // --- DEBUG POUR LE HUD ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // test dégat
            GetComponent<PlayerStats>().TakeDamageServerRpc(10);
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            // test gold
            GetComponent<PlayerStats>().AddGoldServerRpc(50);
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
             if (ChaosManager.Instance != null)
             {
                 ChaosManager.Instance.IncreaseChaos(10f);
             }
        }
    }
}
