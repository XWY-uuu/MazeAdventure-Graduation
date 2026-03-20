using UnityEngine;
using System.Collections.Generic;

public class BlockMazeDecorator : MonoBehaviour
{
    public BlockMazeGenerator mazeGenerator;
    public GameObject groundPrefab;
    public GameObject wallPrefab;
    public GameObject resourcePointPrefab;
    public GameObject enemySpawnPointPrefab;
    public GameObject portalPrefab;
    public int resourcePointCount = 15;
    public int enemySpawnPointCount = 10;

    [Header("玩家出生点设置")]
    [Tooltip("玩家预制体，拖入Inspector面板")]
    public GameObject playerPrefab;
    [Tooltip("玩家出生高度偏移，避免卡入地面")]
    public Vector3 playerSpawnOffset = new Vector3(0, 1f, 0);
    [Tooltip("出生点与传送门的最小距离（网格格数）")]
    public int minDistanceToPortal = 15;
    [Tooltip("出生点与敌人刷新点的最小距离（网格格数）")]
    public int minDistanceToEnemy = 8;
    // 对外暴露的玩家出生点世界坐标，方便其他脚本（如关卡管理、背包系统）调用
    public Vector3 PlayerSpawnWorldPosition { get; private set; }
    // 对外暴露的玩家出生点网格坐标，方便AI、寻路系统调用
    public Vector2Int PlayerSpawnGridPosition { get; private set; }

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

        // ====== 优化1：生成单个大地面Cube ======
        if (groundPrefab != null)
        {
            // 生成单个地面Cube，覆盖整个迷宫区域
            GameObject bigGround = Instantiate(groundPrefab, transform);
            // 计算地面位置：Cube锚点在中心，所以x = 宽度/2 - 0.5，z = 高度/2 - 0.5，y=0.5（让地面表面处于y=0的位置）
            bigGround.transform.position = new Vector3((width - 1) / 2f, 0.5f, (height - 1) / 2f);
            // 计算缩放：宽度=迷宫宽度，高度=1（原有地面高度），深度=迷宫高度
            bigGround.transform.localScale = new Vector3(width, 1f, height);
            // 可选：给大地面命名，方便调试
            bigGround.name = "BigGround";
        }

        // ====== 优化2：合并生成墙壁 ======
        SpawnMergedWalls(grid, width, height);

        // 以下原有逻辑（传送门、玩家、敌人、资源点）保持不变
        Vector2Int portalGridPos = SpawnPortalAndGetPosition(grid, width, height);
        SpawnPlayerStart(grid, width, height, portalGridPos);
        SpawnObjects(enemySpawnPointPrefab, enemySpawnPointCount, grid, width, height);
        SpawnObjects(resourcePointPrefab, resourcePointCount, grid, width, height);

        Debug.Log("迷宫装饰与玩家出生点生成完成！");
    }

    private void SpawnObjects(GameObject prefab, int count, bool[,] grid, int width, int height)
    {
        if (prefab == null || count <= 0) return;

        List<Vector2Int> groundPositions = new List<Vector2Int>();
        // 收集所有地面位置
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

        // 随机选择位置生成
        for (int i = 0; i < count; i++)
        {
            if (groundPositions.Count == 0) break;
            int randomIndex = Random.Range(0, groundPositions.Count);
            Vector2Int pos = groundPositions[randomIndex];
            Vector3 worldPos = new Vector3(pos.x, 0.5f, pos.y);
            Instantiate(prefab, worldPos, Quaternion.identity, transform);
            groundPositions.RemoveAt(randomIndex); // 避免重复生成在同一个位置
        }
    }

    /// <summary>
    /// 生成传送门并返回传送门的网格坐标
    /// </summary>
    private Vector2Int SpawnPortalAndGetPosition(bool[,] grid, int width, int height)
    {
        Vector2Int defaultPortalPos = new Vector2Int(width / 2, height / 2);
        if (portalPrefab == null)
        {
            Debug.LogWarning("未设置传送门预制体");
            return defaultPortalPos;
        }

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

        if (groundPositions.Count == 0)
        {
            Debug.LogError("迷宫无有效地面，无法生成传送门");
            return defaultPortalPos;
        }

        // 随机选择一个位置生成传送门
        Vector2Int portalPos = groundPositions[Random.Range(0, groundPositions.Count)];
        Vector3 worldPos = new Vector3(portalPos.x, 0.5f, portalPos.y);
        Instantiate(portalPrefab, worldPos, Quaternion.identity, transform);

        return portalPos;
    }

    /// <summary>
    /// 生成玩家出生点并实例化玩家
    /// </summary>
    /// <param name="grid">迷宫网格数据</param>
    /// <param name="width">迷宫宽度</param>
    /// <param name="height">迷宫高度</param>
    /// <param name="portalPos">已生成的传送门网格坐标</param>
    private void SpawnPlayerStart(bool[,] grid, int width, int height, Vector2Int portalPos)
    {
        // 异常处理：未设置玩家预制体直接返回
        if (playerPrefab == null)
        {
            Debug.LogError("未设置玩家预制体！请在Inspector面板拖入Player Prefab");
            return;
        }

        // 1. 收集所有合法的地面网格坐标
        List<Vector2Int> allGroundPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y])
                {
                    allGroundPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // 2. 筛选符合距离要求的安全位置
        List<Vector2Int> safeSpawnPositions = new List<Vector2Int>();
        foreach (Vector2Int pos in allGroundPositions)
        {
            // 计算与传送门的曼哈顿距离（网格格数），保证玩家不会出生在终点附近
            int distanceToPortal = Mathf.Abs(pos.x - portalPos.x) + Mathf.Abs(pos.y - portalPos.y);
            if (distanceToPortal >= minDistanceToPortal)
            {
                safeSpawnPositions.Add(pos);
            }
        }

        // 异常处理：无安全位置时降级为全地面随机
        if (safeSpawnPositions.Count == 0)
        {
            Debug.LogWarning("未找到符合距离要求的出生点，已降级为全地面随机生成");
            safeSpawnPositions = allGroundPositions;
        }

        // 3. 随机选择最终出生点
        PlayerSpawnGridPosition = safeSpawnPositions[Random.Range(0, safeSpawnPositions.Count)];
        // 转换为世界坐标，和现有地面/墙壁的坐标体系完全一致
        PlayerSpawnWorldPosition = new Vector3(PlayerSpawnGridPosition.x, 0, PlayerSpawnGridPosition.y) + playerSpawnOffset;

        // 4. 实例化玩家
        Instantiate(playerPrefab, PlayerSpawnWorldPosition, Quaternion.identity);
        Debug.Log($"玩家出生点生成完成！网格坐标：{PlayerSpawnGridPosition}，世界坐标：{PlayerSpawnWorldPosition}");
    }

    /// <summary>
    /// 合并相邻的墙壁格子，生成大Cube（横向优先合并）
    /// </summary>
    private void SpawnMergedWalls(bool[,] grid, int width, int height)
    {
        if (wallPrefab == null) return;

        // 标记是否已处理过该墙壁格子
        bool[,] processed = new bool[width, height];

        // 横向合并墙壁（按行遍历）
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                // 找到未处理的墙壁格子
                if (!grid[x, y] && !processed[x, y])
                {
                    // 向后查找连续的墙壁格子
                    int mergeLength = 1;
                    while (x + mergeLength < width && !grid[x + mergeLength, y] && !processed[x + mergeLength, y])
                    {
                        mergeLength++;
                    }

                    // 生成合并后的大墙壁Cube
                    SpawnMergedWallCube(x, y, mergeLength, 1, width, height);

                    // 标记这些格子为已处理
                    for (int i = 0; i < mergeLength; i++)
                    {
                        processed[x + i, y] = true;
                    }

                    // 跳过已处理的格子
                    x += mergeLength;
                }
                else
                {
                    x++;
                }
            }
        }

        // （可选）纵向合并剩余未处理的墙壁（补充优化）
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

    /// <summary>
    /// 生成单个合并后的墙壁Cube
    /// </summary>
    /// <param name="startX">起始X网格坐标</param>
    /// <param name="startY">起始Y网格坐标</param>
    /// <param name="lengthX">X方向合并长度</param>
    /// <param name="lengthY">Y方向合并长度</param>
    /// <param name="mazeWidth">迷宫总宽度</param>
    /// <param name="mazeHeight">迷宫总高度</param>
    private void SpawnMergedWallCube(int startX, int startY, int lengthX, int lengthY, int mazeWidth, int mazeHeight)
    {
        // 计算大Cube的位置（锚点在中心）
        float posX = startX + (lengthX - 1) / 2f;
        float posZ = startY + (lengthY - 1) / 2f;

        //修改：墙壁位置Y=0.5（中心在0.5，缩放Y=1 → 底部0，顶部1）
        Vector3 spawnPos = new Vector3(posX, 1.5f, posZ);

        // 计算大Cube的缩放
        Vector3 scale = new Vector3(lengthX, 1f, lengthY); // 高度保持1，和原有墙壁一致

        // 生成合并后的墙壁
        GameObject mergedWall = Instantiate(wallPrefab, spawnPos, Quaternion.identity, transform);
        mergedWall.transform.localScale = scale;
        mergedWall.name = $"MergedWall_{startX}_{startY}_Size{lengthX}x{lengthY}";
    }
}