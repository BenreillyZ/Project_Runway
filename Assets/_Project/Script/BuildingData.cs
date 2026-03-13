using UnityEngine;

// This attribute allows you to create new data assets directly from the Unity Project menu.
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Airport/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public string buildingName;    // The name that will be displayed on the Tooltip.
    
    [Header("Economics")]
    public int cost;              // The price per individual grid unit.
    
    [Header("Visuals")]
    public GameObject prefab;     // The actual building model to be instantiated.
    public Sprite icon;           // Icon reserved for future UI buttons.
}
