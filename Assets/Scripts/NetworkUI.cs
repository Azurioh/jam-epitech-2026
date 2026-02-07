using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public static string joinCode = "?";
    [SerializeField] private float uiScale = 1f;
    [SerializeField] private int fontSize = 26;
    [SerializeField] private float buttonHeight = 60f;
    [SerializeField] private float labelHeight = 40f;

    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;

    async void Start()
    {
        // 1. On doit initialiser les services Unity au lancement
        await UnityServices.InitializeAsync();

        // 2. On doit s'authentifier (même anonymement) pour utiliser le Relay
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // --- FONCTION POUR LE HOST ---
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

            // Récupère le CODE (ex: "XJ92") à partager
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Code Join: " + joinCode);

            // Configure le Transport Unity pour utiliser CE serveur Relay
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Lance le Host normalement
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    // --- FONCTION POUR LE CLIENT ---
    public async void JoinRelay(string code)
    {
        try
        {
            // Demande à Unity les infos du serveur associé au code "XJ92"
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            // Configure le Transport Unity pour se connecter à CE serveur
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Lance le Client
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    // --- INTERFACE GRAPHIQUE ---
    void OnGUI()
    {
        // Vérifie que le NetworkManager est prêt
        if (NetworkManager.Singleton == null) return;

        if (_buttonStyle == null || _buttonStyle.fontSize != fontSize)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(uiScale, uiScale, 1f));

        float areaWidth = Mathf.Min(450f, (Screen.width - 20f) / uiScale);
        float areaHeight = Mathf.Min(340f, (Screen.height - 20f) / uiScale);
        GUILayout.BeginArea(new Rect(10f, 10f, areaWidth, areaHeight));

        // Si on n'est pas connecté
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("HOST (Créer une partie)", _buttonStyle, GUILayout.Height(buttonHeight)))
            {
                CreateRelay();
            }

            GUILayout.Space(10); // Petit espace

            GUILayout.Label("Entrer le code pour rejoindre :", _labelStyle, GUILayout.Height(labelHeight));
            joinCode = GUILayout.TextField(joinCode, _labelStyle, GUILayout.Height(labelHeight)); // Champ de texte pour le code

            if (GUILayout.Button("JOIN (Rejoindre avec le code)", _buttonStyle, GUILayout.Height(buttonHeight)))
            {
                JoinRelay(joinCode);
            }
        }

        GUILayout.EndArea();
        GUI.matrix = previousMatrix;
    }
}
