using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // 如果使用 TextMeshPro 处理 UI

public class GridPlacement : MonoBehaviour
{
    [Header("Basic Settings")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Prefabs & Economy")]
    public GameObject[] buildingPrefabs;
    public string[] buildingNames = { "Runway", "Terminal" };
    public int[] buildingCosts = { 500, 2000 };
    public int currentMoney = 10000; // Starting money
    public int selectedIndex = 0;
    
    [Header("Visual & Audio Feedback")]
    public GameObject previewPrefab;
    public GameObject placementEffect; // Particle prefab
    public GameObject deletionEffect;  // Particle prefab
    public AudioClip placementSound;
    public AudioClip deletionSound;
    
    [Header("UI References")]
    public TextMeshProUGUI moneyText;   // Link to your Money UI
    public GameObject tooltipUI;       // A simple panel/text following mouse
    public TextMeshProUGUI tooltipText;

    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    private bool _isPlacementLegal = true;
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;

    void Start()
    {
        if (previewPrefab != null)
        {
            _previewInstance = Instantiate(previewPrefab);
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }
        UpdateMoneyUI();
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        // 1. Tooltip & Deletion Logic
        HandleContextualUI(ray, mousePos);

        if (Mouse.current.rightButton.wasPressedThisFrame)
            HandleDeletion(ray);

        // 2. Placement Logic
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 currentGridPos = SnapToGrid(hit.point);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStartPosition = currentGridPos;
            }

            if (_isDragging) UpdateStretchedPreview(currentGridPos);
            else UpdateSinglePreview(currentGridPos);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                TryPlaceBuildings(currentGridPos);
                ResetDrag();
            }
        }

        // Toggle building type
        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedIndex = 1;
    }

    void HandleContextualUI(Ray ray, Vector2 mousePos)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, obstacleLayer))
        {
            tooltipUI.SetActive(true);
            tooltipUI.transform.position = mousePos + new Vector2(15, 15);
            tooltipText.text = hit.collider.gameObject.name.Replace("(Clone)", "");
        }
        else
        {
            tooltipUI.SetActive(false);
        }
    }

    void TryPlaceBuildings(Vector3 endPos)
    {
        int countX = Mathf.Abs(Mathf.RoundToInt((endPos.x - _dragStartPosition.x) / cellSize)) + 1;
        int countZ = Mathf.Abs(Mathf.RoundToInt((endPos.z - _dragStartPosition.z) / cellSize)) + 1;
        int totalRequired = countX * countZ * buildingCosts[selectedIndex];

        if (currentMoney >= totalRequired && _isPlacementLegal)
        {
            currentMoney -= totalRequired;
            PlaceRepeating(endPos);
            AudioSource.PlayClipAtPoint(placementSound, endPos);
            UpdateMoneyUI();
        }
        else if (currentMoney < totalRequired)
        {
            Debug.Log("Not enough money!");
        }
    }

    void PlaceRepeating(Vector3 end)
    {
        Vector3 start = _dragStartPosition;
        int countX = Mathf.Abs(Mathf.RoundToInt((end.x - start.x) / cellSize));
        int countZ = Mathf.Abs(Mathf.RoundToInt((end.z - start.z) / cellSize));
        float dirX = end.x >= start.x ? 1 : -1;
        float dirZ = end.z >= start.z ? 1 : -1;

        for (int i = 0; i <= countX; i++)
        {
            for (int j = 0; j <= countZ; j++)
            {
                Vector3 spawnPos = start + new Vector3(i * cellSize * dirX, 0, j * cellSize * dirZ);
                if (!Physics.CheckSphere(spawnPos, cellSize * 0.3f, obstacleLayer))
                {
                    GameObject go = Instantiate(buildingPrefabs[selectedIndex], spawnPos, Quaternion.identity);
                    go.name = buildingNames[selectedIndex];
                    go.layer = (int)Mathf.Log(obstacleLayer.value, 2);
                    if (placementEffect) Instantiate(placementEffect, spawnPos, Quaternion.identity);
                }
            }
        }
    }

    void HandleDeletion(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, obstacleLayer))
        {
            if (deletionEffect) Instantiate(deletionEffect, hit.point, Quaternion.identity);
            AudioSource.PlayClipAtPoint(deletionSound, hit.point);
            Destroy(hit.collider.gameObject);
            // Optional: currentMoney += 100; // Refund logic
            UpdateMoneyUI();
        }
    }

    void UpdateMoneyUI() { if (moneyText) moneyText.text = $"Money: ${currentMoney}"; }

    void UpdateSinglePreview(Vector3 pos)
    {
        _previewInstance.transform.position = pos;
        _previewInstance.transform.localScale = Vector3.one;
        _isPlacementLegal = !Physics.CheckSphere(pos, cellSize * 0.3f, obstacleLayer) && (currentMoney >= buildingCosts[selectedIndex]);
        UpdatePreviewColor();
    }

    void UpdateStretchedPreview(Vector3 currentPos)
    {
        Vector3 center = (_dragStartPosition + currentPos) / 2f;
        Vector3 scale = new Vector3(Mathf.Abs(currentPos.x - _dragStartPosition.x) + cellSize, 1f, Mathf.Abs(currentPos.z - _dragStartPosition.z) + cellSize);
        _previewInstance.transform.position = center;
        _previewInstance.transform.localScale = scale;

        int count = (Mathf.Abs(Mathf.RoundToInt((currentPos.x - _dragStartPosition.x) / cellSize)) + 1) * (Mathf.Abs(Mathf.RoundToInt((currentPos.z - _dragStartPosition.z) / cellSize)) + 1);
        _isPlacementLegal = !Physics.CheckBox(center, (scale * 0.95f) / 2f, Quaternion.identity, obstacleLayer) && (currentMoney >= count * buildingCosts[selectedIndex]);
        UpdatePreviewColor();
    }

    void UpdatePreviewColor()
    {
        if (_previewRenderer)
            _previewRenderer.material.SetColor("_BaseColor", _isPlacementLegal ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f));
    }

    void ResetDrag() { _isDragging = false; _previewInstance.transform.localScale = Vector3.one; }

    Vector3 SnapToGrid(Vector3 pos) => new Vector3(Mathf.Round(pos.x / cellSize) * cellSize, 0.1f, Mathf.Round(pos.z / cellSize) * cellSize);
}
