using UnityEngine;

[RequireComponent(typeof(CharacterController))] // 强制挂载CharacterController，避免穿模
public class PlayerController : MonoBehaviour
{
    // 原有moveSpeed字段替换为以下内容
    [Header("移动设置")]
    [Tooltip("基础移动速度（迷宫场景建议8-12）")] public float baseMoveSpeed = 10f;
    [HideInInspector] public float currentMoveSpeed; // 装备系统实时修改
    [Tooltip("移动平滑度（0=无平滑，1=瞬间移动，建议0.1）")] public float moveSmoothTime = 0.1f;
    [Tooltip("重力加速度（默认9.81即可）")] public float gravity = 9.81f;

    [Header("视角设置")]
    [Tooltip("鼠标水平灵敏度（左右旋转）")]
    public float mouseX_Sensitivity = 2f;
    [Tooltip("鼠标垂直灵敏度（上下旋转）")]
    public float mouseY_Sensitivity = 2f;
    [Tooltip("相机上下旋转最大角度（避免翻倒）")]
    public float maxLookAngle = 80f;
    [Tooltip("主相机（拖入场景中的MainCamera）")]
    public Camera mainCamera;

    // 私有变量（平滑移动/视角用）
    private CharacterController characterController;
    private Vector3 currentMoveVelocity;
    private Vector3 smoothMoveVelocity;
    private float currentCameraRotationX = 0f; // 相机垂直旋转角度

    void Start()
    {
        // 初始化组件
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("玩家对象缺少CharacterController组件！");
        }

        // 初始化相机（如果未手动赋值，自动查找主相机）
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            Debug.LogWarning("未手动赋值主相机，已自动查找MainCamera");
        }

        // 锁定鼠标到屏幕中心，隐藏光标（游戏时更沉浸）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentMoveSpeed = baseMoveSpeed;
    }

    void Update()
    {
        // 核心逻辑：先处理视角，再处理移动
        HandleCameraRotation();
        HandlePlayerMovement();
    }

    /// <summary>
    /// 处理鼠标视角旋转
    /// </summary>
    private void HandleCameraRotation()
    {
        // 获取鼠标输入（Input.GetAxisRaw避免平滑，更跟手）
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseX_Sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseY_Sensitivity;

        // 1. 玩家自身绕Y轴旋转（左右视角）
        transform.Rotate(Vector3.up * mouseX);

        // 2. 相机绕X轴旋转（上下视角），限制角度
        currentCameraRotationX -= mouseY;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -maxLookAngle, maxLookAngle); // 限制-80~80度
        mainCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }

    /// <summary>
    /// 处理WASD移动 + 重力
    /// </summary>
    private void HandlePlayerMovement()
    {
        // 1. 获取WASD输入（-1~1的数值）
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D ↔
        float inputZ = Input.GetAxisRaw("Vertical"); // W/S ↕

        // 2. 转换为世界空间的移动方向（基于玩家当前朝向，只绕Y轴）
        Vector3 moveDirection = new Vector3(inputX, 0f, inputZ).normalized; // 归一化避免斜向移动更快
        moveDirection = transform.TransformDirection(moveDirection); // 把本地方向转世界方向

        // 3. 平滑移动（让移动更丝滑，无卡顿）
        currentMoveVelocity = Vector3.SmoothDamp(currentMoveVelocity, moveDirection * currentMoveSpeed, ref smoothMoveVelocity, moveSmoothTime);
        // 4. 重力处理（防止玩家浮空）
        if (characterController.isGrounded) // 只有落地时重置垂直速度
        {
            currentMoveVelocity.y = -1f; // 轻微向下力，确保贴地
        }
        else
        {
            currentMoveVelocity.y -= gravity * Time.deltaTime; // 空中受重力
        }

        // 5. 执行移动（CharacterController的核心方法）
        characterController.Move(currentMoveVelocity * Time.deltaTime);
    }

    // 可选：按ESC解锁鼠标（方便调试/暂停）
    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0)) // 按鼠标左键重新锁定
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 调试用：在Scene视图绘制玩家移动方向
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, currentMoveVelocity.normalized * 2f);
    }
}