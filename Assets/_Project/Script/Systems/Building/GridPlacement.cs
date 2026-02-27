using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GridPlacement : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Prefabs & Selection")]
    public GameObject[] buildingPrefabs; // Array to store different types (0: Runway, 1: Terminal, etc.)
    public int selectedIndex = 0;
    public GameObject previewPrefab;
    
    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    
    [Header("Validation & Feedback")]
    public Color validColor = new Color(0, 1, 0, 0.5f);
    public Color invalidColor = new Color(1, 0, 0, 0.5f);
    private bool _isPlacementLegal = true;

    [Header("Drag-to-Build")]
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;

    void Start()
    {
        if (previewPrefab != null)
        {
            _previewInstance = Instantiate(previewPrefab);
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        // 1. Handle Deletion (Right Click)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleDeletion(ray);
        }

        // 2. Handle Placement Logic
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 currentGridPos = SnapToGrid(hit.point);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStartPosition = currentGridPos;
            }

            if (_isDragging)
                UpdateStretchedPreview(currentGridPos);
            else
                UpdateSinglePreview(currentGridPos);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (_isPlacementLegal) PlaceRepeatingBuildings(currentGridPos);
                ResetDrag();
            }
        }
        
        // Quick Toggle Selection (Alpha 1, 2, 3...)
        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedIndex = 1;
    }

    void UpdateStretchedPreview(Vector3 currentPos)
    {
        Vector3 center = (_dragStartPosition + currentPos) / 2f;
        Vector3 scale = new Vector3(
            Mathf.Abs(currentPos.x - _dragStartPosition.x) + cellSize,
            1f,
            Mathf.Abs(currentPos.z - _dragStartPosition.z) + cellSize
        );

        _previewInstance.transform.position = center;
        _previewInstance.transform.localScale = scale;
        
        _isPlacementLegal = !Physics.CheckBox(center, (scale * 0.95f) / 2f, Quaternion.identity, obstacleLayer);
        UpdatePreviewColor();
    }

    // New logic: Instantiate individual units instead of scaling a single mesh
    void PlaceRepeatingBuildings(Vector3 endPos)
    {
        Vector3 start = _dragStartPosition;
        Vector3 end = endPos;

        // Calculate how many units to place along X and Z
        int countX = Mathf.Abs(Mathf.RoundToInt((end.x - start.x) / cellSize));
        int countZ = Mathf.Abs(Mathf.RoundToInt((end.z - start.z) / cellSize));

        float dirX = end.x >= start.x ? 1 : -1;
        float dirZ = end.z >= start.z ? 1 : -1;

        for (int i = 0; i <= countX; i++)
        {
            for (int j = 0; j <= countZ; j++)
            {
                Vector3 spawnPos = start + new Vector3(i * cellSize * dirX, 0, j * cellSize * dirZ);
                // Check once more to avoid overlapping self
                if (!Physics.CheckSphere(spawnPos, cellSize * 0.3f, obstacleLayer))
                {
                    GameObject go = Instantiate(buildingPrefabs[selectedIndex], spawnPos, Quaternion.identity);
                    go.layer = Mathf.Internal_ClosestLayerIndex(obstacleLayer.value);
                }
            }
        }
    }

    void HandleDeletion(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f, obstacleLayer))
        {
            Destroy(hit.collider.gameObject);
        }
    }

    void UpdateSinglePreview(Vector3 pos)
    {
        _previewInstance.transform.position = pos;
        _previewInstance.transform.localScale = Vector3.one;
        _isPlacementLegal = !Physics.CheckSphere(pos, cellSize * 0.3f, obstacleLayer);
        UpdatePreviewColor();
    }

    void UpdatePreviewColor()
    {
        if (_previewRenderer != null)
            _previewRenderer.material.SetColor("_BaseColor", _isPlacementLegal ? validColor : invalidColor);
    }

    void ResetDrag()
    {
        _isDragging = false;
        _previewInstance.transform.localScale = Vector3.one;
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;
        return new Vector3(x, 0.1f, z);
    }
}
