using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manages saving and loading the game state (placed buildings + economy) to a JSON file.
/// Listens to EventBus.OnSaveRequested and EventBus.OnLoadRequested.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("The same BuildingData array used by GridPlacement, so we can match names back to prefabs.")]
    public BuildingData[] availableBuildings;

    [Tooltip("The layer used to identify placed buildings in the scene.")]
    public LayerMask buildingLayer;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.OnSaveRequested += SaveGame;
        EventBus.OnLoadRequested += LoadGame;
    }

    private void OnDisable()
    {
        EventBus.OnSaveRequested -= SaveGame;
        EventBus.OnLoadRequested -= LoadGame;
    }

    // ────────────────────────── Save ──────────────────────────

    public void SaveGame()
    {
        SaveData data = new SaveData();

        // Save economy state
        if (EconomyManager.Instance != null)
        {
            data.money = EconomyManager.Instance.currentMoney;
        }

        // Collect all buildings on the building layer
        // We find all GameObjects with an ObjectPoolTag (they were spawned from the pool or instantiated via our system)
        ObjectPoolTag[] tags = FindObjectsOfType<ObjectPoolTag>();
        foreach (var tag in tags)
        {
            GameObject go = tag.gameObject;
            // Only include active objects on the correct layer
            if (!go.activeInHierarchy) continue;
            if (((1 << go.layer) & buildingLayer.value) == 0) continue;

            BuildingSaveEntry entry = new BuildingSaveEntry
            {
                buildingDataName = go.name,
                posX = go.transform.position.x,
                posY = go.transform.position.y,
                posZ = go.transform.position.z,
                rotationY = go.transform.eulerAngles.y
            };
            data.buildings.Add(entry);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[SaveLoadManager] Game saved! {data.buildings.Count} buildings written to {SaveFilePath}");
    }

    // ────────────────────────── Load ──────────────────────────

    public void LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("[SaveLoadManager] No save file found.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogError("[SaveLoadManager] Failed to parse save data.");
            return;
        }

        // 1. Clear existing buildings
        ClearAllBuildings();

        // 2. Restore economy
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.currentMoney = data.money;
            EventBus.OnMoneyChanged?.Invoke(data.money);
        }

        // 3. Respawn buildings
        foreach (var entry in data.buildings)
        {
            BuildingData buildingData = FindBuildingDataByName(entry.buildingDataName);
            if (buildingData == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Could not find BuildingData named '{entry.buildingDataName}'. Skipping.");
                continue;
            }

            Vector3 pos = new Vector3(entry.posX, entry.posY, entry.posZ);
            Quaternion rot = Quaternion.Euler(0, entry.rotationY, 0);

            GameObject go;
            if (ObjectPoolManager.Instance != null)
            {
                go = ObjectPoolManager.Instance.SpawnFromPool(buildingData.prefab, pos, rot);
            }
            else
            {
                go = Object.Instantiate(buildingData.prefab, pos, rot);
            }

            go.name = buildingData.buildingName;
            go.layer = LayerMaskToLayer(buildingLayer);
        }

        Debug.Log($"[SaveLoadManager] Game loaded! {data.buildings.Count} buildings restored.");
    }

    private void ClearAllBuildings()
    {
        ObjectPoolTag[] tags = FindObjectsOfType<ObjectPoolTag>();
        foreach (var tag in tags)
        {
            GameObject go = tag.gameObject;
            if (!go.activeInHierarchy) continue;
            if (((1 << go.layer) & buildingLayer.value) == 0) continue;

            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.ReturnToPool(go);
            else
                Destroy(go);
        }
    }

    private BuildingData FindBuildingDataByName(string buildingName)
    {
        if (availableBuildings == null) return null;
        foreach (var bd in availableBuildings)
        {
            if (bd != null && bd.buildingName == buildingName)
                return bd;
        }
        return null;
    }

    /// <summary>
    /// Converts a LayerMask (bitmask) into a single layer index. Assumes only one bit is set.
    /// </summary>
    private static int LayerMaskToLayer(LayerMask mask)
    {
        return (int)Mathf.Log(mask.value, 2);
    }
}

// ────────────────────────── Serializable Data Structures ──────────────────────────

[System.Serializable]
public class SaveData
{
    public int money;
    public List<BuildingSaveEntry> buildings = new List<BuildingSaveEntry>();
}

[System.Serializable]
public class BuildingSaveEntry
{
    public string buildingDataName;
    public float posX;
    public float posY;
    public float posZ;
    public float rotationY;
}
