using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using ProjectUtilities; // Added for the new Vector3Extensions

public class GridPlacement : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Building Data System")]
    // Drag your BuildingData assets (Runway, Terminal, etc.) into this array in the Inspector
    public BuildingData[] availableBuildings; 
    public int selectedIndex = 0;
    
    [Header("Input Actions")]
    public InputAction placeAction;
    public InputAction deleteAction;
    public InputAction pointerAction;
    public InputAction[] selectBuildingActions;

    [Header("Visual & Audio Feedback")]
    public GameObject previewPrefab;
    public GameObject placementEffect;
    public AudioClip placementSound;
    public AudioClip deletionSound;

    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    private bool _isPlacementLegal = true;
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;
    private Camera _mainCamera;

    private void Awake()
    {
        if (placeAction == null || placeAction.bindings.Count == 0) placeAction = new InputAction("Place", binding: "<Mouse>/leftButton");
        if (deleteAction == null || deleteAction.bindings.Count == 0) deleteAction = new InputAction("Delete", binding: "<Mouse>/rightButton");
        if (pointerAction == null || pointerAction.bindings.Count == 0) pointerAction = new InputAction("Pointer", binding: "<Mouse>/position");
        
        if (selectBuildingActions == null || selectBuildingActions.Length == 0)
        {
            selectBuildingActions = new InputAction[9];
            for (int i = 0; i < 9; i++)
            {
                selectBuildingActions[i] = new InputAction($"Select{i+1}", binding: $"<Keyboard>/{i+1}");
            }
        }
    }

    private void OnEnable()
    {
        placeAction.Enable();
        deleteAction.Enable();
        pointerAction.Enable();
        if (selectBuildingActions != null)
        {
            foreach (var action in selectBuildingActions) action?.Enable();
        }
    }

    private void OnDisable()
    {
        placeAction.Disable();
        deleteAction.Disable();
        pointerAction.Disable();
        if (selectBuildingActions != null)
        {
            foreach (var action in selectBuildingActions) action?.Disable();
        }
    }

    void Start()
    {
        _mainCamera = Camera.main;
        if (previewPrefab != null)
        {
            _previewInstance = Instantiate(previewPrefab);
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }
    }

    void Update()
    {
        Vector2 mousePos = pointerAction.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        // 1. Tooltip and Deletion logic optimized to one raycast
        if (Physics.Raycast(ray, out RaycastHit obstacleHit, 1000f, obstacleLayer))
        {
            EventBus.OnTooltipStateChanged?.Invoke(true, obstacleHit.collider.gameObject.name, mousePos);
            if (deleteAction.WasPressedThisFrame())
            {
                HandleDeletion(obstacleHit);
            }
        }
        else
        {
            EventBus.OnTooltipStateChanged?.Invoke(false, "", mousePos);
        }

        // 2. Placement Logic
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 currentGridPos = SnapToGrid(hit.point);

            if (placeAction.WasPressedThisFrame())
            {
                _isDragging = true;
                _dragStartPosition = currentGridPos;
            }

            if (_isDragging) UpdateStretchedPreview(currentGridPos);
            else UpdateSinglePreview(currentGridPos);

            if (placeAction.WasReleasedThisFrame())
            {
                TryPlaceBuildings(currentGridPos);
                ResetDrag();
            }
        }

        // Quick toggle building types with keys 1, 2, 3...
        for (int i = 0; i < availableBuildings.Length; i++)
        {
            if (i < selectBuildingActions.Length && selectBuildingActions[i] != null && selectBuildingActions[i].WasPressedThisFrame())
            {
                selectedIndex = i;
            }
        }
    }

    void TryPlaceBuildings(Vector3 endPos)
    {
        if (availableBuildings.Length == 0) return;
        
        BuildingData data = availableBuildings[selectedIndex];
        int countX = Mathf.Abs(Mathf.RoundToInt((endPos.x - _dragStartPosition.x) / cellSize)) + 1;
        int countZ = Mathf.Abs(Mathf.RoundToInt((endPos.z - _dragStartPosition.z) / cellSize)) + 1;
        int totalRequired = countX * countZ * data.cost;

        if (_isPlacementLegal)
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(totalRequired))
            {
                // Fire an event to request money deduction instead of directly calling EconomyManager.Instance.SpendMoney
                EventBus.OnRequestSpendMoney?.Invoke(totalRequired);
                
                PlaceRepeating(endPos, data);
                AudioSource.PlayClipAtPoint(placementSound, endPos);
            }
            else if (EconomyManager.Instance == null)
            {
                // Fallback for testing without EconomyManager
                Debug.LogWarning("EconomyManager not present in scene. Bypassing cost.");
                PlaceRepeating(endPos, data);
                AudioSource.PlayClipAtPoint(placementSound, endPos);
            }
        }
    }

    void PlaceRepeating(Vector3 end, BuildingData data)
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
                    GameObject go;
                    if (ObjectPoolManager.Instance != null)
                    {
                        go = ObjectPoolManager.Instance.SpawnFromPool(data.prefab, spawnPos, Quaternion.identity);
                    }
                    else
                    {
                        go = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                    }
                    
                    go.name = data.buildingName; 
                    go.layer = (int)Mathf.Log(obstacleLayer.value, 2);
                    
                    if (placementEffect) 
                    {
                        if (ObjectPoolManager.Instance != null)
                            ObjectPoolManager.Instance.SpawnFromPool(placementEffect, spawnPos, Quaternion.identity);
                        else
                            Instantiate(placementEffect, spawnPos, Quaternion.identity);
                    }

                    // Fire an event that a building was placed
                    EventBus.OnBuildingPlaced?.Invoke(spawnPos, data);
                }
            }
        }
    }

    void HandleDeletion(RaycastHit hit)
    {
        AudioSource.PlayClipAtPoint(deletionSound, hit.point);
        Vector3 point = hit.point;
        
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(hit.collider.gameObject);
        else
            Destroy(hit.collider.gameObject);
            
        // Fire an event that a building was deleted
        EventBus.OnBuildingDeleted?.Invoke(point);
    }

    void UpdateSinglePreview(Vector3 pos)
    {
        _previewInstance.transform.position = pos;
        _previewInstance.transform.localScale = Vector3.one;
        _isPlacementLegal = !Physics.CheckSphere(pos, cellSize * 0.3f, obstacleLayer);
        UpdatePreviewColor();
    }

    void UpdateStretchedPreview(Vector3 currentPos)
    {
        Vector3 center = (_dragStartPosition + currentPos) / 2f;
        Vector3 scale = new Vector3(Mathf.Abs(currentPos.x - _dragStartPosition.x) + cellSize, 1f, Mathf.Abs(currentPos.z - _dragStartPosition.z) + cellSize);
        _previewInstance.transform.position = center;
        _previewInstance.transform.localScale = scale;

        _isPlacementLegal = !Physics.CheckBox(center, (scale * 0.95f) / 2f, Quaternion.identity, obstacleLayer);
        UpdatePreviewColor();
    }

    void UpdatePreviewColor()
    {
        if (_previewRenderer)
            _previewRenderer.material.SetColor("_BaseColor", _isPlacementLegal ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f));
    }

    void ResetDrag() { _isDragging = false; _previewInstance.transform.localScale = Vector3.one; }

    Vector3 SnapToGrid(Vector3 pos) => pos.SnapToGrid(cellSize, 0.1f);
}
