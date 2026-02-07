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
    private string statusMessage = "";
    private bool isConnecting = false;
    private bool codeCopied = false;
    private float codeCopiedTimer = 0f;

    // // Référence à la caméra principale de la scène (pour la désactiver/réactiver)
    // public static Camera sceneMainCamera;

    private Texture2D _panelBackground;
    private Texture2D _buttonNormal;
    private Texture2D _buttonHover;
    private Texture2D _inputBackground;
    private Texture2D _accentTexture;

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _inputStyle;
    private GUIStyle _codeDisplayStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _smallButtonStyle;

    private bool _stylesInitialized = false;
    private int _lastScreenHeight = 0;

    async void Start()
    {
        statusMessage = "Connexion aux services...";

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            statusMessage = "Prêt à jouer !";
        }
        catch (System.Exception e)
        {
            statusMessage = "Erreur: " + e.Message;
            Debug.LogError(e);
        }
    }

    void Update()
    {
        if (codeCopied)
        {
            codeCopiedTimer -= Time.deltaTime;
            if (codeCopiedTimer <= 0)
                codeCopied = false;
        }
    }

    private void CreateTextures()
    {
        _panelBackground = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.95f));

        _buttonNormal = MakeTexture(2, 2, new Color(0.35f, 0.25f, 0.7f, 1f));
        _buttonHover = MakeTexture(2, 2, new Color(0.45f, 0.35f, 0.85f, 1f));

        _inputBackground = MakeTexture(2, 2, new Color(0.15f, 0.15f, 0.2f, 1f));

        _accentTexture = MakeTexture(2, 2, new Color(0.1f, 0.8f, 0.6f, 0.2f));
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private Texture2D MakeGradientTexture(int width, int height, Color top, Color bottom)
    {
        Texture2D texture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            Color color = Color.Lerp(bottom, top, (float)y / height);
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }
        texture.Apply();
        return texture;
    }

    private int S(int value)
    {
        float scale = Screen.height / 1080f;
        return Mathf.RoundToInt(value * scale);
    }

    private void InitStyles()
    {
        if (_panelBackground == null)
        {
            CreateTextures();
        }

        if (_stylesInitialized && Screen.height == _lastScreenHeight) return;
        _lastScreenHeight = Screen.height;

        // Panel style
        _panelStyle = new GUIStyle();
        _panelStyle.normal.background = _panelBackground;
        _panelStyle.padding = new RectOffset(S(30), S(30), S(30), S(30));

        // Title style
        _titleStyle = new GUIStyle();
        _titleStyle.fontSize = S(42);
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.alignment = TextAnchor.MiddleCenter;
        _titleStyle.normal.textColor = Color.white;
        _titleStyle.margin = new RectOffset(0, 0, 0, S(20));

        // Label style
        _labelStyle = new GUIStyle();
        _labelStyle.fontSize = S(20);
        _labelStyle.alignment = TextAnchor.MiddleLeft;
        _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f, 1f);
        _labelStyle.margin = new RectOffset(0, 0, S(10), S(5));

        // Button style
        _buttonStyle = new GUIStyle();
        _buttonStyle.fontSize = S(24);
        _buttonStyle.fontStyle = FontStyle.Bold;
        _buttonStyle.alignment = TextAnchor.MiddleCenter;
        _buttonStyle.normal.background = _buttonNormal;
        _buttonStyle.normal.textColor = Color.white;
        _buttonStyle.hover.background = _buttonHover;
        _buttonStyle.hover.textColor = Color.white;
        _buttonStyle.active.background = _buttonHover;
        _buttonStyle.active.textColor = new Color(0.9f, 0.9f, 1f, 1f);
        _buttonStyle.padding = new RectOffset(S(20), S(20), S(15), S(15));
        _buttonStyle.margin = new RectOffset(0, 0, S(10), S(10));

        // Small button style
        _smallButtonStyle = new GUIStyle(_buttonStyle);
        _smallButtonStyle.fontSize = S(18);
        _smallButtonStyle.padding = new RectOffset(S(15), S(15), S(10), S(10));

        // Input style
        _inputStyle = new GUIStyle();
        _inputStyle.fontSize = S(28);
        _inputStyle.fontStyle = FontStyle.Bold;
        _inputStyle.alignment = TextAnchor.MiddleCenter;
        _inputStyle.normal.background = _inputBackground;
        _inputStyle.normal.textColor = Color.white;
        _inputStyle.focused.background = _inputBackground;
        _inputStyle.focused.textColor = new Color(0.5f, 1f, 0.8f, 1f);
        _inputStyle.padding = new RectOffset(S(15), S(15), S(15), S(15));
        _inputStyle.margin = new RectOffset(0, 0, S(5), S(15));

        // Code display style
        _codeDisplayStyle = new GUIStyle();
        _codeDisplayStyle.fontSize = S(48);
        _codeDisplayStyle.fontStyle = FontStyle.Bold;
        _codeDisplayStyle.alignment = TextAnchor.MiddleCenter;
        _codeDisplayStyle.normal.background = _accentTexture;
        _codeDisplayStyle.normal.textColor = new Color(0.2f, 1f, 0.7f, 1f);
        _codeDisplayStyle.padding = new RectOffset(S(20), S(20), S(20), S(20));
        _codeDisplayStyle.margin = new RectOffset(0, 0, S(10), S(10));

        // Status style
        _statusStyle = new GUIStyle();
        _statusStyle.fontSize = S(16);
        _statusStyle.alignment = TextAnchor.MiddleCenter;
        _statusStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f, 1f);
        _statusStyle.margin = new RectOffset(0, 0, S(20), 0);

        _stylesInitialized = true;
    }

    public async void CreateRelay()
    {
        if (isConnecting) return;
        isConnecting = true;
        statusMessage = "Création de la partie...";

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Code Join: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            DisableSceneMainCamera();
            statusMessage = "Partie créée !";
        }
        catch (System.Exception e)
        {
            statusMessage = "Erreur: " + e.Message;
            Debug.LogError(e);
        }

        isConnecting = false;
    }

    public async void JoinRelay(string code)
    {
        if (isConnecting || string.IsNullOrEmpty(code)) return;
        isConnecting = true;
        statusMessage = "Connexion en cours...";

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            DisableSceneMainCamera();
            statusMessage = "Connecté !";
        }
        catch (System.Exception e)
        {
            statusMessage = "Erreur: Code invalide";
            Debug.LogError(e);
        }

        isConnecting = false;
    }

    /// <summary>
    /// Désactive la caméra principale de la scène pour éviter les conflits avec la caméra du joueur.
    /// </summary>
    private void DisableSceneMainCamera()
    {
        // Trouver la caméra de la scène (taggée MainCamera qui n'est pas celle du joueur)
        // Camera[] cameras = Camera.allCameras;
        // foreach (Camera cam in cameras)
        // {
        //     if (cam.CompareTag("MainCamera"))
        //     {
        //         sceneMainCamera = cam;
        //         cam.gameObject.SetActive(false);
        //         Debug.Log("[NetworkUI] Main camera de la scène désactivée");
        //         break;
        //     }
        // }
    }

    /// <summary>
    /// Réactive la caméra principale de la scène (pour le menu pause).
    /// </summary>
    public static void EnableSceneMainCamera()
    {
        // if (sceneMainCamera != null)
        // {
        //     sceneMainCamera.gameObject.SetActive(true);
        //     Debug.Log("[NetworkUI] Main camera de la scène réactivée");
        // }
    }

    /// <summary>
    /// Désactive la caméra principale de la scène (quand on ferme le menu pause).
    /// </summary>
    public static void DisableSceneMainCameraStatic()
    {
        // if (sceneMainCamera != null)
        // {
        //     sceneMainCamera.gameObject.SetActive(false);
        //     Debug.Log("[NetworkUI] Main camera de la scène désactivée");
        // }
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        InitStyles();

        bool isInGame = NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;

        if (!isInGame)
        {
            // Menu principal centré
            float panelWidth = S(500);
            float panelHeight = S(420);
            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight), _panelStyle);

            GUILayout.Label("MULTIJOUEUR", _titleStyle);

            GUILayout.Space(S(10));

            GUI.enabled = !isConnecting;
            if (GUILayout.Button("CRÉER UNE PARTIE", _buttonStyle, GUILayout.Height(S(60))))
            {
                CreateRelay();
            }

            GUILayout.Space(S(20));

            GUILayout.Label("Entrer le code de la partie :", _labelStyle);
            joinCode = GUILayout.TextField(joinCode.ToUpper(), 6, _inputStyle, GUILayout.Height(S(50)));

            if (GUILayout.Button("REJOINDRE", _buttonStyle, GUILayout.Height(S(60))))
            {
                JoinRelay(joinCode);
            }
            GUI.enabled = true;

            GUILayout.EndArea();
        }
    }
}
