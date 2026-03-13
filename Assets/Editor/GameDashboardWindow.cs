using UnityEngine;
using UnityEditor;

public class GameDashboardWindow : EditorWindow
{
    private int startingMoney = 10000;

    [MenuItem("Project Runway/Dashboard", false, 0)]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<GameDashboardWindow>("Runway Dashboard");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Management Dashboard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Economy Settings
        GUILayout.Label("Economy", EditorStyles.boldLabel);
        startingMoney = EditorGUILayout.IntField("Starting Money", startingMoney);
        
        if (GUILayout.Button("Apply Starting Money to Manager"))
        {
            EconomyManager manager = FindObjectOfType<EconomyManager>();
            if (manager != null)
            {
                Undo.RecordObject(manager, "Update Starting Money");
                manager.currentMoney = startingMoney;
                EditorUtility.SetDirty(manager);
                Debug.Log($"[Dashboard] Set starting money to {startingMoney}!");
            }
            else
            {
                Debug.LogWarning("[Dashboard] Could not find an EconomyManager in the scene.");
            }
        }
        
        EditorGUILayout.Space();

        // 2. Scene Utilities
        GUILayout.Label("Scene Utilities", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear All Placed Buildings"))
        {
            if (EditorUtility.DisplayDialog("Clear Buildings", "Are you sure you want to delete all objects marked as buildings?", "Yes", "Cancel"))
            {
                Transform poolManager = FindObjectOfType<ObjectPoolManager>()?.transform;
                if (poolManager != null)
                {
                    // In a real scenario, we might want to clear the pool queue or search for specific layers
                    int count = 0;
                    for (int i = poolManager.childCount - 1; i >= 0; i--)
                    {
                        Undo.DestroyObjectImmediate(poolManager.GetChild(i).gameObject);
                        count++;
                    }
                    Debug.Log($"[Dashboard] Cleared {count} buildings from the scene pool.");
                }
                else
                {
                    Debug.LogWarning("[Dashboard] No ObjectPoolManager found to clear objects from.");
                }
            }
        }
    }
}
