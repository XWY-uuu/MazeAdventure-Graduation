using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("核心引用（自动查找/手动拖入）")]
    public BlockMazeDecorator mazeDecorator;
    public BlockMazeGenerator mazeGenerator;

    [Header("敌人配置")]
    public GameObject enemyPrefab;
    [Tooltip("是否开启全局敌人数量上限")]
    public bool useGlobalEnemyLimit = false;
    [Tooltip("全局最大敌人数量（仅开启上限时生效）")]
    public int maxGlobalEnemyCount = 50;

    private int _currentEnemyCount;
    private GameObject _player;

    private void Awake()
    {
        // 自动查找组件
        if (mazeDecorator == null)
            mazeDecorator = Object.FindFirstObjectByType<BlockMazeDecorator>();
        if (mazeGenerator == null)
            mazeGenerator = Object.FindFirstObjectByType<BlockMazeGenerator>();

        // 查找玩家
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.CompareTag("Player"))
            {
                _player = obj;
                break;
            }
        }

        if (_player == null)
        {
            Debug.LogError("EnemySpawner找不到玩家！请确保玩家对象的Tag设置为Player");
        }

        // 监听迷宫生成事件
        BlockMazeManager mazeManager = Object.FindFirstObjectByType<BlockMazeManager>();
        if (mazeManager != null)
        {
            mazeManager.OnMazeGenerated += SpawnEnemies;
        }

        // 异常提示
        if (mazeDecorator == null || mazeGenerator == null)
        {
            Debug.LogError("EnemySpawner找不到迷宫相关组件！请检查场景里的Maze物体");
        }
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner未设置敌人预制体！请把Enemy预制体拖入对应字段");
        }
    }

    public void SpawnEnemies()
    {
        ClearAllEnemies();
        _currentEnemyCount = 0;

        // 筛选有效房间
        List<Room> validRooms = mazeGenerator.AllGeneratedRooms.FindAll(r =>
            r.center != mazeDecorator.PlayerSpawnGridPosition &&
            r.center != mazeDecorator.PortalGridPosition
        );

        if (validRooms.Count == 0)
        {
            Debug.LogWarning("没有找到可生成敌人的有效房间！");
            return;
        }

        foreach (Room room in validRooms)
        {
            if (useGlobalEnemyLimit && _currentEnemyCount >= maxGlobalEnemyCount)
            {
                Debug.Log($"已达到全局敌人数量上限{maxGlobalEnemyCount}，停止生成");
                break;
            }

            // 计算敌人数量
            int baseEnemyCount = Mathf.RoundToInt((float)room.Area / mazeDecorator.enemyPerGridArea);
            int finalEnemyCount = baseEnemyCount + Random.Range(-mazeDecorator.enemyRandomOffset, mazeDecorator.enemyRandomOffset + 1);
            finalEnemyCount = Mathf.Clamp(finalEnemyCount, mazeDecorator.minEnemyPerRoom, mazeDecorator.maxEnemyPerRoom);

            if (useGlobalEnemyLimit)
            {
                finalEnemyCount = Mathf.Min(finalEnemyCount, maxGlobalEnemyCount - _currentEnemyCount);
            }

            SpawnEnemiesInRoom(room, finalEnemyCount);
            _currentEnemyCount += finalEnemyCount;
            Debug.Log($"房间[{room.center}]已生成{finalEnemyCount}个敌人");
        }

        Debug.Log($"敌人刷新完成！本次共生成{_currentEnemyCount}个敌人，有效房间数量：{validRooms.Count}");
    }

    private void SpawnEnemiesInRoom(Room room, int count)
    {
        List<Vector2Int> roomGround = mazeGenerator.GetGroundPositionsInRoom(room);
        if (roomGround.Count == 0 || count <= 0 || enemyPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            if (roomGround.Count == 0) break;

            int randomIdx = Random.Range(0, roomGround.Count);
            Vector2Int gridPos = roomGround[randomIdx];
            Vector3 worldPos = new Vector3(gridPos.x, 0f, gridPos.y);

            // ========== 修复：强制初始旋转为0，不会有倾斜 ==========
            GameObject enemy = Instantiate(enemyPrefab, worldPos, Quaternion.identity, transform);
            enemy.name = $"Enemy_{gridPos.x}_{gridPos.y}";
            enemy.tag = "Enemy";

            // 强制重置敌人的旋转，彻底解决倾斜
            enemy.transform.rotation = Quaternion.identity;

            // 初始化AI数据
            EnemyBehaviorTree enemyBT = enemy.GetComponent<EnemyBehaviorTree>();
            if (enemyBT != null && enemyBT.Blackboard != null)
            {
                enemyBT.Blackboard.health = 100f;
                enemyBT.Blackboard.viewRange = 15f;
                enemyBT.Blackboard.attackRange = 2f;
                enemyBT.Blackboard.moveSpeed = 5f;
                enemyBT.Blackboard.fleeSpeed = 7f;
                enemyBT.Blackboard.player = _player;
            }

            // 刷新寻路网格
            JPSOptimization pathfinder = enemy.GetComponent<JPSOptimization>();
            pathfinder?.RefreshGrid();

            roomGround.RemoveAt(randomIdx);
        }
    }

    private void ClearAllEnemies()
    {
        GameObject[] enemies = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in enemies)
        {
            if (obj.CompareTag("Enemy"))
            {
                Destroy(obj);
            }
        }
        _currentEnemyCount = 0;
    }
}