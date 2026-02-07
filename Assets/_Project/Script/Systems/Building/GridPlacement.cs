using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacement : MonoBehaviour
{
    [Header("设置")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer; // 检查：Inspector 面板里这里选了什么？

    [Header("预览")]
    public GameObject previewPrefab;
    private GameObject _previewInstance;

    void Start()
    {
        if (previewPrefab != null && _previewInstance == null)
        {
            _previewInstance = Instantiate(previewPrefab);
        }
    }

    void Update()
    {
        // 1. 获取鼠标位置
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // 2. 发射射线
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        // 【调试用】在 Scene 窗口画出一根红线，看看射线射向哪里
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            // 3. 计算对齐后的位置
            Vector3 snappedPos = SnapToGrid(hit.point);
            
            // 4. 移动预览物体
            if (_previewInstance != null)
            {
                _previewInstance.transform.position = snappedPos;
            }
        }
        else
        {
            // 如果没射中地面，打印一下原因
            // Debug.Log("射线没有击中任何 LayerMask 指定的地面");
        }
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;
        // 注意：0.1f 是为了让网格稍微浮在地面上方，避免闪烁
        return new Vector3(x, 0.1f, z);
    }
}