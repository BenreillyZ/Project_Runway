using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovementNewInput : MonoBehaviour
{
    // --- 1. 输入动作定义 ---
    [Header("Input Actions (输入绑定)")]
    public InputAction moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
    public InputAction verticalAction = new InputAction("Vertical");
    public InputAction rotateAction = new InputAction("Rotate");
    public InputAction orbitActiveAction = new InputAction("OrbitActive", binding: "<Mouse>/middleButton");
    public InputAction lookAction = new InputAction("Look", binding: "<Mouse>/delta");
    public InputAction zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll/y");

    // --- 2. 参数设置 ---
    [Header("Movement Settings (移动设置)")]
    public float moveSpeed = 10.0f;
    
    [Tooltip("移动惯性：值越大滑行越久。建议 0.1(灵敏) - 0.3(顺滑)")]
    public float moveSmoothing = 0.2f; // 这里的含义变成了“滑动阻力系数”

    [Header("Rotation & Orbit (旋转设置)")]
    public float rotationSpeed = 80.0f;
    public float mouseSensitivity = 0.5f;
    public float defaultFocusDistance = 15.0f;

    [Header("Zoom Settings (缩放设置)")]
    public float zoomSensitivity = 0.05f;
    public float zoomDamping = 5.0f;

    [Header("Bounds (边界限制)")]
    public bool enableBounds = true;
    public Vector2 heightLimit = new Vector2(1f, 50f);

    // --- 内部状态 ---
    private float currentZoomVelocity = 0.0f;
    private bool isOrbiting = false;
    private Vector3 lockedFocusPoint;
    
    // 【修改】移除了 moveVelocitySmoothing (SmoothDamp 专用变量，已不再需要)
    private Vector3 currentMoveDir = Vector3.zero; 

    private void Awake()
    {
        // 自动绑定保护逻辑
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }
        if (verticalAction.bindings.Count == 0)
        {
             verticalAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/f")
                .With("Negative", "<Keyboard>/c");
        }
        if (rotateAction.bindings.Count == 0)
        {
             rotateAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/e")
                .With("Negative", "<Keyboard>/q");
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        verticalAction.Enable();
        rotateAction.Enable();
        orbitActiveAction.Enable();
        lookAction.Enable();
        zoomAction.Enable();

        orbitActiveAction.started += OnOrbitStarted;
        orbitActiveAction.canceled += OnOrbitCanceled;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        verticalAction.Disable();
        rotateAction.Disable();
        orbitActiveAction.Disable();
        lookAction.Disable();
        zoomAction.Disable();

        orbitActiveAction.started -= OnOrbitStarted;
        orbitActiveAction.canceled -= OnOrbitCanceled;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleOrbit();
        HandleZoomInput();
    }

    void LateUpdate()
    {
        ApplyZoomPhysics();
        ApplyBounds();
    }

    // --- 【修改】修复了回抽问题的移动逻辑 ---
    void HandleMovement()
    {
        // 1. 读取输入
        Vector2 inputRaw = moveAction.ReadValue<Vector2>();
        float vInputRaw = verticalAction.ReadValue<float>();

        // 2. 计算目标方向
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        
        Vector3 targetMoveDir = (forward.normalized * inputRaw.y + right.normalized * inputRaw.x);
        targetMoveDir += Vector3.up * vInputRaw;

        // 3. 【核心修复】改用 Lerp (线性插值) 替代 SmoothDamp
        // SmoothDamp 像弹簧，会回弹；Lerp 像摩擦力滑行，绝不回弹。
        
        // 计算插值速度：moveSmoothing 越小，Lerp 越快 (越灵敏)
        // 使用 Time.deltaTime 保证帧率无关
        float lerpSpeed = 1.0f / Mathf.Max(0.001f, moveSmoothing); 
        
        currentMoveDir = Vector3.Lerp(currentMoveDir, targetMoveDir, Time.deltaTime * lerpSpeed);

        // 4. 应用移动
        // 增加一个极小值截断，防止无限趋近于 0 但不归零导致的微小浮动
        if (currentMoveDir.sqrMagnitude < 0.001f && targetMoveDir == Vector3.zero)
        {
            currentMoveDir = Vector3.zero;
        }

        if (currentMoveDir.sqrMagnitude > 0.0001f)
        {
            transform.position += currentMoveDir * moveSpeed * Time.deltaTime;
        }
    }

    void HandleRotation()
    {
        float rInput = rotateAction.ReadValue<float>();
        if (rInput != 0f)
        {
            Vector3 focus = GetCurrentFocusPoint();
            float angle = rInput * rotationSpeed * Time.deltaTime;
            transform.RotateAround(focus, Vector3.up, angle);
        }
    }

    private void OnOrbitStarted(InputAction.CallbackContext ctx)
    {
        isOrbiting = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lockedFocusPoint = GetCurrentFocusPoint();
    }

    private void OnOrbitCanceled(InputAction.CallbackContext ctx)
    {
        isOrbiting = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HandleOrbit()
    {
        if (isOrbiting)
        {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            float mouseX = mouseDelta.x * mouseSensitivity; 
            float mouseY = mouseDelta.y * mouseSensitivity;

            transform.RotateAround(lockedFocusPoint, Vector3.up, mouseX);
            transform.RotateAround(lockedFocusPoint, transform.right, -mouseY);
        }
    }

    void HandleZoomInput()
    {
        float scrollValue = zoomAction.ReadValue<float>();
        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            currentZoomVelocity += scrollValue * zoomSensitivity;
        }
    }

    void ApplyZoomPhysics()
    {
        if (Mathf.Abs(currentZoomVelocity) > 0.001f)
        {
            transform.position += transform.forward * currentZoomVelocity * Time.deltaTime;
            currentZoomVelocity = Mathf.Lerp(currentZoomVelocity, 0f, zoomDamping * Time.deltaTime);
        }
    }

    void ApplyBounds()
    {
        if (!enableBounds) return;
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, heightLimit.x, heightLimit.y);
        transform.position = pos;
    }

    private Vector3 GetCurrentFocusPoint()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return hit.point;
        }
        return transform.position + transform.forward * defaultFocusDistance;
    }
}