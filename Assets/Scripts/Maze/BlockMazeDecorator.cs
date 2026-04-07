using UnityEngine;
using System.Collections.Generic;

public class BlockMazeDecorator : MonoBehaviour
{
    public BlockMazeGenerator mazeGenerator;
    public GameObject groundPrefab;
    public GameObject wallPrefab;
    public GameObject resourcePointPrefab;
    public GameObject portalPrefab;
    public int resourcePointCount = 15;

    [Header("敌人生成设置")]
    [Tooltip("每多少格房间面积生成1个敌人（例：100=每100格面积1个敌人）")]
    public int enemyPerGridArea = 100;
    [Tooltip("敌人数量随机波动范围（例：2=最终数量±2）")]
    public int enemyRandomOffset = 2;
    [Tooltip("单个房间最大敌人数量")]
    public int maxEnemyPerRoom = 10;
    [Tooltip("单个房间最小敌人数量")]
    public int minEnemyPerRoom = 1;

    [Header("玩家出生点设置")]
    [Tooltip("玩家预制体（仅首次生成无玩家时使用）")]
    public GameObject playerPrefab;
    [Tooltip("玩家出生高度偏移，避免卡入地面")]
    public Vector3 playerSpawnOffset = new Vector3(0, 1f, 0);
    // 对外暴露的玩家出生点世界坐标，方便其他脚本调用
    public Vector3 PlayerSpawnWorldPosition { get; private set; }
    // 对外暴露的玩家出生点网格坐标，方便AI、寻路系统调用
    public Vector2Int PlayerSpawnGridPosition { get; private set; }
    // ========== 新增：对外暴露传送门网格坐标 ==========
    public Vector2Int PortalGridPosition { get; private set; }

    // 存储玩家和传送门所在的房间
    private Room _playerRoom;
    private Room _portalRoom;

    public void DecorateMaze()
    {
        // 先清除现有子物体
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        bool[,] grid = mazeGenerator.GetGrid();
        int width = mazeGenerator.mazeWidth;
        int height = mazeGenerator.mazeHeight;

        // 生成单个大地面Cube
        if (groundPrefab != null)
        {
            GameObject bigGround = Instantiate(groundPrefab, transform);
            bigGround.transform.position = new Vector3((width - 1) / 2f, -0.5f, (height - 1) / 2f);
            bigGround.transform.localScale = new Vector3(width, 1f, height);
            bigGround.name = "BigGround";
        }

        // 合并生成墙壁
        SpawnMergedWalls(grid, width, height);

        // 1. 获取迷宫中最远的两个房间，分别分配给玩家和传送门
        var (roomA, roomB) = mazeGenerator.GetFarthestRoomPair();
        _playerRoom = roomA;
        _portalRoom = roomB;

        // 2. 生成传送门
        SpawnPortalInFixedRoom();
        // 3. 生成玩家出生点
        SpawnPlayerInFixedRoom();
        // 4. 生成资源点
        SpawnObjects(resourcePointPrefab, resourcePointCount, grid, width, height);

        Debug.Log("迷宫装饰完成！玩家与传送门已分配至最远房间");
    }

    // 生成传送门
    private void SpawnPortalInFixedRoom()
    {
        if (portalPrefab == null)
        {
            Debug.LogWarning("未设置传送门预制体");
            return;
        }

        PortalGridPosition = _portalRoom.center;
        Vector3 worldPos = new Vector3(PortalGridPosition.x, 0.5f, PortalGridPosition.y);
        Instantiate(portalPrefab, worldPos, Quaternion.identity, transform);

        Debug.Log($"传送门已生成在房间中心，网格坐标：{PortalGridPosition}");
    }

    // 生成/移动玩家
    private void SpawnPlayerInFixedRoom()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("未设置玩家预制体！请在Inspector面板拖入Player Prefab");
            return;
        }

        PlayerSpawnGridPosition = _playerRoom.center;
        PlayerSpawnWorldPosition = new Vector3(PlayerSpawnGridPosition.x, 0, PlayerSpawnGridPosition.y) + playerSpawnOffset;

        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            existingPlayer.transform.position = PlayerSpawnWorldPosition;
            existingPlayer.transform.rotation = Quaternion.identity;
            Debug.Log($"已有玩家已移动到最远房间！网格坐标：{PlayerSpawnGridPosition}");
        }
        else
        {
            Instantiate(playerPrefab, PlayerSpawnWorldPosition, Quaternion.identity);
            Debug.Log($"场景中未找到玩家，已在最远房间创建新玩家！");
        }
    }

    // 通用生成方法（资源点用）
    private void SpawnObjects(GameObject prefab, int count, bool[,] grid, int width, int height)
    {
        if (prefab == null || count <= 0) return;

        List<Vector2Int> groundPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y])
                {
                    groundPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (groundPositions.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            if (groundPositions.Count == 0) break;
            int randomIndex = Random.Range(0, groundPositions.Count);
            Vector2Int pos = groundPositions[randomIndex];
            Vector3 worldPos = new Vector3(pos.x, 0.5f, pos.y);
            Instantiate(prefab, worldPos, Quaternion.identity, transform);
            groundPositions.RemoveAt(randomIndex);
        }
    }

    // 合并墙壁
    private void SpawnMergedWalls(bool[,] grid, int width, int height)
    {
        if (wallPrefab == null) return;
        bool[,] processed = new bool[width, height];

        // 横向合并
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                if (!grid[x, y] && !processed[x, y])
                {
                    int mergeLength = 1;
                    while (x + mergeLength < width && !grid[x + mergeLength, y] && !processed[x + mergeLength, y])
                    {
                        mergeLength++;
                    }
                    SpawnMergedWallCube(x, y, mergeLength, 1, width, height);
                    for (int i = 0; i < mergeLength; i++)
                    {
                        processed[x + i, y] = true;
                    }
                    x += mergeLength;
                }
                else
                {
                    x++;
                }
            }
        }

        // 纵向合并
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                if (!grid[x, y] && !processed[x, y])
                {
                    int mergeHeight = 1;
                    while (y + mergeHeight < height && !grid[x, y + mergeHeight] && !processed[x, y + mergeHeight])
                    {
                        mergeHeight++;
                    }
                    SpawnMergedWallCube(x, y, 1, mergeHeight, width, height);
                    for (int i = 0; i < mergeHeight; i++)
                    {
                        processed[x, y + i] = true;
                    }
                    y += mergeHeight;
                }
                else
                {
                    y++;
                }
            }
        }
    }

    // 生成合并后的墙壁Cube
    private void SpawnMergedWallCube(int startX, int startY, int lengthX, int lengthY, int mazeWidth, int mazeHeight)
    {
        float posX = startX + (lengthX - 1) / 2f;
        float posZ = startY + (lengthY - 1) / 2f;
        Vector3 spawnPos = new Vector3(posX, 1f, posZ);
        Vector3 scale = new Vector3(lengthX, 2f, lengthY);

        GameObject mergedWall = Instantiate(wallPrefab, spawnPos, Quaternion.identity, transform);
        mergedWall.transform.localScale = scale;
        mergedWall.name = $"MergedWall_{startX}_{startY}_Size{lengthX}x{lengthY}";
    }
}