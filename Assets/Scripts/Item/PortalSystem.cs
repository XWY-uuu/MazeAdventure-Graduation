using UnityEngine;

public class PortalSystem : MonoBehaviour
{
    [Header("传送门配置")]
    [Tooltip("交互距离")] public float interactRange = 3f;
    [Tooltip("是否需要清理所有敌人才能进入")] public bool needClearAllEnemies = true;

    [Header("状态")]
    public bool isPortalActive = false;
    private GameObject _player;
    private BlockMazeManager _mazeManager;
    private EnemySpawner _enemySpawner;

    [Header("可视化")]
    [SerializeField] private GameObject activeEffect;
    [SerializeField] private GameObject inactiveEffect;

    private void Awake()
    {
        // 自动获取核心组件
        _player = GameObject.FindGameObjectWithTag("Player");
        _mazeManager = Object.FindFirstObjectByType<BlockMazeManager>();
        _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();

        // 监听敌人全清事件
        if (_enemySpawner != null)
        {
            _enemySpawner.OnAllEnemiesCleared += ActivatePortal;
        }

        // 初始化状态
        isPortalActive = !needClearAllEnemies;
        UpdatePortalState();
    }

    private void OnDestroy()
    {
        // 取消事件监听，防止内存泄漏
        if (_enemySpawner != null)
        {
            _enemySpawner.OnAllEnemiesCleared -= ActivatePortal;
        }
    }

    private void Update()
    {
        // 实时检测敌人是否全清
        if (needClearAllEnemies && !isPortalActive && _enemySpawner != null)
        {
            if (_enemySpawner.currentAliveEnemyCount <= 0)
            {
                ActivatePortal();
            }
        }

        // 玩家交互检测
        if (isPortalActive && _player != null)
        {
            float distance = Vector3.Distance(transform.position, _player.transform.position);
            if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
            {
                EnterNextLevel();
            }
        }
    }

    // 激活传送门
    public void ActivatePortal()
    {
        isPortalActive = true;
        UpdatePortalState();
        Debug.Log("所有敌人已清理，传送门已激活！");
    }

    // 进入下一层关卡
    private void EnterNextLevel()
    {
        if (!isPortalActive) return;

        Debug.Log("进入下一层迷宫！");
        // 1. 关卡层数+1
        _mazeManager.currentLevel++;

        // 2. 重新生成迷宫与敌人
        _mazeManager.GenerateCompleteMaze();

        // 3. 玩家状态已通过DontDestroyOnLoad保留，无需额外处理
        // 4. 传送门位置会随迷宫重新生成自动更新
    }

    // 更新传送门视觉状态
    private void UpdatePortalState()
    {
        if (activeEffect != null)
        {
            activeEffect.SetActive(isPortalActive);
        }
        if (inactiveEffect != null)
        {
            inactiveEffect.SetActive(!isPortalActive);
        }
    }

    // 场景内绘制交互范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isPortalActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}