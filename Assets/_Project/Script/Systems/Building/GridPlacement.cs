using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacement : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer; // Layer for existing buildings

    [Header("Prefabs")]
    public GameObject buildingPrefab; // The actual building to be placed
    public GameObject previewPrefab;  // The transparent preview model
    
    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    
    [Header("Validation Colors")]
    public Color validColor = new Color(0, 1, 0, 0.5f);   // Green
    public Color invalidColor = new Color(1, 0, 0, 0.5f); // Red
    private bool _isPlacementLegal = true;

    [Header("Drag-to-Build")]
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;

    void Start()
    {
        if (previewPrefab != null)
        {
            _previewInstance = Instantiate(previewPrefab);
            // Assuming the renderer is on the object or its children
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 currentGridPos = SnapToGrid(hit.point);

            // 1. Handle Input for Dragging
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragStartPosition = currentGridPos;
            }

            // 2. Update Preview (Single or Stretched)
            if (_isDragging)
            {
                UpdateStretchedPreview(currentGridPos);
            }
            else
            {
                UpdateSinglePreview(currentGridPos);
            }

            // 3. Substantial Construction (Placement)
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (_isPlacementLegal)
                {
                    PlaceBuilding();
                }
                ResetDrag();
            }
        }
    }

    void UpdateSinglePreview(Vector3 pos)
    {
        _previewInstance.transform.position = pos;
        _previewInstance.transform.localScale = Vector3.one;
        ValidatePosition(pos, Vector3.one);
    }

    void UpdateStretchedPreview(Vector3 currentPos)
    {
        // Calculate center and scale for runways/fences
        Vector3 center = (_dragStartPosition + currentPos) / 2f;
        Vector3 scale = new Vector3(
            Mathf.Abs(currentPos.x - _dragStartPosition.x) + cellSize,
            1f,
            Mathf.Abs(currentPos.z - _dragStartPosition.z) + cellSize
        );

        _previewInstance.transform.position = center;
        _previewInstance.transform.localScale = scale;
        
        ValidatePosition(center, scale);
    }

    void ValidatePosition(Vector3 center, Vector3 scale)
    {
        // Physics check for overlapping obstacles
        _isPlacementLegal = !Physics.CheckBox(center, (scale * 0.95f) / 2f, Quaternion.identity, obstacleLayer);

        // Dynamic Shader Feedback
        if (_previewRenderer != null)
        {
            _previewRenderer.material.SetColor("_BaseColor", _isPlacementLegal ? validColor : invalidColor);
        }
    }

    void PlaceBuilding()
    {
        // Instantiate the substantial building based on preview's transform
        GameObject finalBuilding = Instantiate(buildingPrefab, _previewInstance.transform.position, Quaternion.identity);
        finalBuilding.transform.localScale = _previewInstance.transform.localScale;
        
        // Ensure the new building is on the Obstacle layer
        finalBuilding.layer = (int)Mathf.Log(obstacleLayer.value, 2);
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
