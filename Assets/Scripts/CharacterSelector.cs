using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    public static CharacterSelector Instance;

    [Header("Character Prefabs")]
    public GameObject[] characterPrefabs;

    [Header("Carousel Settings")]
    public Transform carouselCenter;
    public float characterSpacing = 2f;
    public float depthOffset = 0.5f;
    public float rotationSpeed = 8f;
    public float selectedScale = 1.2f;
    public float unselectedScale = 0.8f;

    [Header("Scene Settings")]
    public string gameSceneName = "NathanGame";

    private int _currentIndex = 0;
    private GameObject[] _characterPreviews;
    private float _targetRotation = 0f;
    private float _currentRotation = 0f;
    private Camera _mainCamera;

    public static int SelectedCharacterIndex { get; private set; } = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _mainCamera = Camera.main;


        DisableSceneCameraControls();

        SpawnAllCharacters();
        UpdateCarousel();
    }

    private void DisableSceneCameraControls()
    {

        var cameraControllers = FindObjectsOfType<CinemachineMouseController>();
        foreach (var controller in cameraControllers)
        {

            if (controller.transform.parent == null || !controller.transform.parent.name.Contains("Preview"))
            {
                Destroy(controller);
            }
        }
    }

    void Update()
    {

        _currentRotation = Mathf.Lerp(_currentRotation, _targetRotation, Time.deltaTime * rotationSpeed);


        UpdateCharacterPositions();
        LookAtCamera();
    }

    private void SpawnAllCharacters()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;

        _characterPreviews = new GameObject[characterPrefabs.Length];

        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            if (characterPrefabs[i] != null)
            {
                Vector3 spawnPos = carouselCenter != null ? carouselCenter.position : Vector3.zero;
                _characterPreviews[i] = Instantiate(characterPrefabs[i], spawnPos, Quaternion.identity);
                _characterPreviews[i].name = $"CharacterPreview_{i}";

                DisableGameplayComponents(_characterPreviews[i]);
            }
        }
    }

    private void UpdateCharacterPositions()
    {
        if (_characterPreviews == null) return;

        int count = _characterPreviews.Length;
        if (count == 0) return;

        Vector3 centerPos = carouselCenter != null ? carouselCenter.position : Vector3.zero;

        float totalWidth = (count - 1) * characterSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            if (_characterPreviews[i] == null) continue;

            bool isSelected = (i == _currentIndex);

            float xPos = startX + i * characterSpacing;

            float zPos = isSelected ? depthOffset : 0f;

            Vector3 targetPos = centerPos + new Vector3(xPos, 0, zPos);

            _characterPreviews[i].transform.position = Vector3.Lerp(
                _characterPreviews[i].transform.position,
                targetPos,
                Time.deltaTime * rotationSpeed
            );

            float targetScale = isSelected ? selectedScale : unselectedScale;

            Vector3 currentScale = _characterPreviews[i].transform.localScale;
            float newScale = Mathf.Lerp(currentScale.x, targetScale, Time.deltaTime * rotationSpeed);
            _characterPreviews[i].transform.localScale = Vector3.one * newScale;
        }
    }

    private void LookAtCamera()
    {
        if (_mainCamera == null || _characterPreviews == null) return;

        foreach (var preview in _characterPreviews)
        {
            if (preview == null) continue;

            Vector3 directionToCamera = _mainCamera.transform.position - preview.transform.position;
            directionToCamera.y = 0;

            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                preview.transform.rotation = Quaternion.Slerp(
                    preview.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f
                );
            }
        }
    }

    private void UpdateCarousel()
    {
    }

    public void NextCharacter()
    {
        if (characterPrefabs.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % characterPrefabs.Length;
        UpdateCarousel();
    }

    public void PreviousCharacter()
    {
        if (characterPrefabs.Length == 0) return;
        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = characterPrefabs.Length - 1;
        UpdateCarousel();
    }

    private void DisableGameplayComponents(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        var networkBehaviours = obj.GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>();
        foreach (var nb in networkBehaviours) nb.enabled = false;

        var cameraControllers = obj.GetComponentsInChildren<CinemachineMouseController>();
        foreach (var controller in cameraControllers)
        {
            controller.enabled = false;
            Destroy(controller);
        }

        var cameras = obj.GetComponentsInChildren<Camera>();
        foreach (var cam in cameras)
        {
            cam.enabled = false;
        }

        var audioListeners = obj.GetComponentsInChildren<AudioListener>();
        foreach (var listener in audioListeners)
        {
            listener.enabled = false;
        }
    }

    public void ConfirmSelection()
    {
        SelectedCharacterIndex = _currentIndex;
        Debug.Log($"Character selected: {characterPrefabs[_currentIndex].name} (Index: {_currentIndex})");

        SceneManager.LoadScene(gameSceneName);
    }

    public string GetCurrentCharacterName()
    {
        if (characterPrefabs.Length > 0 && characterPrefabs[_currentIndex] != null)
        {
            string fullName = characterPrefabs[_currentIndex].name;
            return fullName.Replace("player ", "").Replace("Player ", "");
        }
        return "Unknown";
    }

    void OnDestroy()
    {

        if (_characterPreviews != null)
        {
            foreach (var preview in _characterPreviews)
            {
                if (preview != null) Destroy(preview);
            }
        }
    }
}
