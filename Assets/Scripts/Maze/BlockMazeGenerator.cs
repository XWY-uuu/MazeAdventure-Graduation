using UnityEngine;
using System.Collections.Generic;

public class BlockMazeGenerator : MonoBehaviour
{
    public int mazeWidth = 30;
    public int mazeHeight = 30;
    public int seed = 12345;
    public bool useRandomSeedOnStart = true; // 是否在启动时使用随机种子，每次生成不同的迷宫
    [Range(0.1f, 0.8f)]
    public float groundRatio = 0.6f; // 地面占比，0.6表示60%是地面
    private bool[,] grid; // true是地面，false是墙壁

    public bool[,] GetGrid()
    {
        return grid;
    }

    public void GenerateMaze()
    {
        // 如果开启了随机种子，就生成一个随机的种子
        if (useRandomSeedOnStart)
        {
            seed = Random.Range(0, 999999);
            Debug.Log($"使用随机种子生成迷宫，种子为：{seed}");
        }
        Random.InitState(seed);
        grid = new bool[mazeWidth, mazeHeight];

        // 初始化所有格子为墙壁
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                grid[x, y] = false;
            }
        }

        // 随机漫步生成地面，保证连通性
        int startX = Random.Range(1, mazeWidth - 1);
        int startY = Random.Range(1, mazeHeight - 1);
        grid[startX, startY] = true;

        Stack<Vector2Int> walkStack = new Stack<Vector2Int>();
        walkStack.Push(new Vector2Int(startX, startY));

        int totalGround = Mathf.RoundToInt(mazeWidth * mazeHeight * groundRatio);
        int currentGround = 1;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (currentGround < totalGround && walkStack.Count > 0)
        {
            Vector2Int current = walkStack.Peek();
            List<Vector2Int> neighbors = new List<Vector2Int>();

            // 查找周围的墙壁格子
            foreach (Vector2Int dir in directions)
            {
                int newX = current.x + dir.x;
                int newY = current.y + dir.y;
                if (newX >= 0 && newX < mazeWidth && newY >= 0 && newY < mazeHeight && !grid[newX, newY])
                {
                    neighbors.Add(new Vector2Int(newX, newY));
                }
            }

            if (neighbors.Count > 0)
            {
                // 随机选择一个邻居作为地面
                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                grid[chosen.x, chosen.y] = true;
                currentGround++;
                walkStack.Push(chosen);
            }
            else
            {
                // 没有邻居，回溯
                walkStack.Pop();
            }
        }

        // 边缘设置为墙壁，防止玩家走出地图
        for (int x = 0; x < mazeWidth; x++)
        {
            grid[x, 0] = false;
            grid[x, mazeHeight - 1] = false;
        }
        for (int y = 0; y < mazeHeight; y++)
        {
            grid[0, y] = false;
            grid[mazeWidth - 1, y] = false;
        }

        Debug.Log($"方块迷宫生成完成，地面数量：{currentGround}，总格子数：{mazeWidth * mazeHeight}，种子：{seed}");
    }

    // 检测格子是否是地面
    public bool IsGround(int x, int y)
    {
        if (x >= 0 && x < mazeWidth && y >= 0 && y < mazeHeight)
        {
            return grid[x, y];
        }
        return false;
    }

    /// <summary>
    /// 获取迷宫中所有合法地面的网格坐标
    /// </summary>
    public List<Vector2Int> GetAllGroundPositions()
    {
        List<Vector2Int> groundPositions = new List<Vector2Int>();
        if (grid == null) return groundPositions;

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (grid[x, y])
                {
                    groundPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        return groundPositions;
    }

    /// <summary>
    /// 获取一个随机的合法地面网格坐标
    /// </summary>
    public Vector2Int GetRandomGroundPosition()
    {
        List<Vector2Int> groundPositions = GetAllGroundPositions();
        if (groundPositions.Count == 0) return new Vector2Int(1, 1);
        return groundPositions[Random.Range(0, groundPositions.Count)];
    }
}