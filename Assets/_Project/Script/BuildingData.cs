using UnityEngine;

// This attribute allows you to create new data assets directly from the Unity Project menu.
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Airport/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public string buildingName;    // The name that will be displayed on the Tooltip.
    [TextArea(2, 4)]
    public string description;     // A longer description shown in detailed tooltips or info panels.
    
    [Header("Economics")]
    public int cost;              // The price per individual grid unit.
    public int incomePerMinute;   // Revenue this building generates per minute (for future economy loop).
    
    [Header("Placement")]
    public Vector2Int size = Vector2Int.one;  // Grid footprint in cells (e.g. 2x3 for a terminal).
    public bool canRotate = true;             // Whether this building type supports rotation.
    
    [Header("Visuals")]
    public GameObject prefab;     // The actual building model to be instantiated.
    public Sprite icon;           // Icon reserved for future UI buttons.
}
