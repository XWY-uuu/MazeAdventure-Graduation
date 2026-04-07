using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 房间结构体，新增圆润化相关参数
public struct Room
{
    public int x;          // 房间左下角X坐标
    public int y;          // 房间左下角Y坐标
    public int width;      // 房间宽度
    public int height;     // 房间高度
    public int cornerRadius; // 房间圆角半径（圆润化核心参数）
    public Vector2Int center; // 房间中心点
    // 新增：房间面积，用于计算敌人数量
    public int Area => width * height;
}

// 辅助类：最小生成树用的边
public struct MazeEdge
{
    public int roomAIndex;
    public int roomBIndex;
    public float distance;
}

public class BlockMazeGenerator : MonoBehaviour
{
    [Header("迷宫基础尺寸")]
    public int mazeWidth = 300;
    public int mazeHeight = 300;
    public int seed = 12345;
    public bool useRandomSeedOnStart = true;

    [Header("细胞房间设置")]
    [Tooltip("生成房间的最大尝试次数")]
    public int maxRoomSpawnTryCount = 120;
    [Tooltip("房间最小宽度（含）")]
    public int minRoomWidth = 20;
    [Tooltip("房间最大宽度（含）")]
    public int maxRoomWidth = 70;
    [Tooltip("房间最小高度（含）")]
    public int minRoomHeight = 20;
    [Tooltip("房间最大高度（含）")]
    public int maxRoomHeight = 70;
    [Tooltip("房间之间的最小间隔（格数）")]
    public int roomMinSpacing = 5;

    [Header("房间圆润化设置")]
    [Tooltip("房间圆角最小半径")]
    public int minCornerRadius = 3;
    [Tooltip("房间圆角最大半径")]
    public int maxCornerRadius = 10;
    [Tooltip("房间边缘自然扰动强度（0=无扰动，越大边缘越不规则）")]
    [Range(0, 5)] public int roomEdgeNoise = 2;

    [Header("道路设置")]
    [Tooltip("道路宽度，必须≥5")]
    public int roadWidth = 5;
    [Tooltip("道路扭曲强度（0=无扭曲，越大越蜿蜒）")]
    [Range(0, 30)] public int roadTwistIntensity = 15;
    [Tooltip("道路平滑度（越大越平滑，越小转折越多）")]
    [Range(2, 10)] public int roadSmoothStep = 5;

    // 核心网格数据，完全兼容原有逻辑：true=地面，false=墙壁
    private bool[,] grid;
    // ========== 修改1：把房间列表改为public，对外暴露，方便其他脚本访问 ==========
    public List<Room> AllGeneratedRooms { get; private set; } = new List<Room>();

    #region 原有对外接口（完全保留，100%兼容）
    public bool[,] GetGrid() => grid;

    public bool IsGround(int x, int y)
    {
        if (x >= 0 && x < mazeWidth && y >= 0 && y < mazeHeight)
            return grid[x, y];
        return false;
    }

    public List<Vector2Int> GetAllGroundPositions()
    {
        List<Vector2Int> groundPositions = new List<Vector2Int>();
        if (grid == null) return groundPositions;
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                if (grid[x, y])
                    groundPositions.Add(new Vector2Int(x, y));
        return groundPositions;
    }

    public Vector2Int GetRandomGroundPosition()
    {
        List<Vector2Int> groundPositions = GetAllGroundPositions();
        return groundPositions.Count == 0 ? new Vector2Int(1, 1) : groundPositions[Random.Range(0, groundPositions.Count)];
    }

    public Vector2Int GetRandomRoomCenter()
    {
        return AllGeneratedRooms.Count == 0 ? new Vector2Int(mazeWidth / 2, mazeHeight / 2) : AllGeneratedRooms[Random.Range(0, AllGeneratedRooms.Count)].center;
    }
    #endregion

    // ========== 修改2：新增核心方法：获取迷宫中距离最远的两个房间 ==========
    /// <summary>
    /// 遍历所有房间两两配对，返回距离最远的两个房间
    /// </summary>
    /// <returns>长度为2的数组，[0]和[1]为最远的两个房间</returns>
    public (Room roomA, Room roomB) GetFarthestRoomPair()
    {
        // 异常处理：房间不足2个时返回默认值
        if (AllGeneratedRooms.Count < 2)
        {
            Debug.LogError("房间数量不足2个，无法生成最远房间对！");
            return (new Room(), new Room());
        }

        float maxDistance = 0;
        Room farthestRoomA = AllGeneratedRooms[0];
        Room farthestRoomB = AllGeneratedRooms[1];

        // 遍历所有房间两两配对，找最大距离
        for (int i = 0; i < AllGeneratedRooms.Count; i++)
        {
            for (int j = i + 1; j < AllGeneratedRooms.Count; j++)
            {
                // 用欧几里得距离计算房间中心的直线距离，保证全局最远
                float distance = Vector2Int.Distance(AllGeneratedRooms[i].center, AllGeneratedRooms[j].center);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestRoomA = AllGeneratedRooms[i];
                    farthestRoomB = AllGeneratedRooms[j];
                }
            }
        }

        Debug.Log($"已找到最远房间对，距离：{maxDistance}格");
        return (farthestRoomA, farthestRoomB);
    }

    // ========== 修改3：新增方法：获取指定房间内的所有合法地面坐标 ==========
    /// <summary>
    /// 获取指定房间内部的所有地面坐标，用于生成敌人/资源
    /// </summary>
    public List<Vector2Int> GetGroundPositionsInRoom(Room room)
    {
        List<Vector2Int> roomGroundPositions = new List<Vector2Int>();

        // 遍历房间的所有格子
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                // 只保留合法的地面格子（排除圆角外的墙壁、边缘扰动的墙壁）
                if (x >= 0 && x < mazeWidth && y >= 0 && y < mazeHeight && grid[x, y])
                {
                    roomGroundPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        return roomGroundPositions;
    }

    public void GenerateMaze()
    {
        // 种子初始化，兼容原有逻辑
        if (useRandomSeedOnStart)
        {
            seed = Random.Range(0, 999999);
            Debug.Log($"使用随机种子生成圆润房间+扭曲道路迷宫，种子为：{seed}");
        }
        Random.InitState(seed);

        // 1. 初始化网格：全部设为墙壁
        grid = new bool[mazeWidth, mazeHeight];
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                grid[x, y] = false;

        AllGeneratedRooms.Clear();

        // 2. 核心：生成带圆角的圆润房间
        GenerateRoundedRooms();

        // 3. 核心：用最小生成树生成无重复的房间连接，再生成扭曲道路
        ConnectRoomsWithTwistedRoad();

        // 4. 迷宫边缘强制设为墙壁，防止玩家出界
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

        Debug.Log($"迷宫生成完成！房间数量：{AllGeneratedRooms.Count}，道路宽度：{roadWidth}格");
    }

    #region 第一步：生成带圆角的圆润房间
    /// <summary>
    /// 生成不重叠、带圆角的圆润房间
    /// </summary>
    private void GenerateRoundedRooms()
    {
        for (int i = 0; i < maxRoomSpawnTryCount; i++)
        {
            // 随机生成房间尺寸
            int roomWidth = Random.Range(minRoomWidth, maxRoomWidth + 1);
            int roomHeight = Random.Range(minRoomHeight, maxRoomHeight + 1);
            // 随机生成圆角半径（不超过房间最小尺寸的1/3，避免圆角挖空房间）
            int cornerRadius = Random.Range(minCornerRadius, Mathf.Min(maxCornerRadius, Mathf.Min(roomWidth, roomHeight) / 3));

            // 随机生成房间位置（保证不超出边界）
            int roomX = Random.Range(5, mazeWidth - roomWidth - 5);
            int roomY = Random.Range(5, mazeHeight - roomHeight - 5);

            Room newRoom = new Room()
            {
                x = roomX,
                y = roomY,
                width = roomWidth,
                height = roomHeight,
                cornerRadius = cornerRadius,
                center = new Vector2Int(roomX + roomWidth / 2, roomY + roomHeight / 2)
            };

            // 检查房间是否重叠
            if (IsRoomOverlap(newRoom)) continue;

            // 把带圆角的房间区域标记为地面
            DrawRoundedRoom(newRoom);

            AllGeneratedRooms.Add(newRoom);
        }
    }

    /// <summary>
    /// 绘制带圆角的房间，实现圆润效果
    /// </summary>
    private void DrawRoundedRoom(Room room)
    {
        int right = room.x + room.width;
        int top = room.y + room.height;
        int r = room.cornerRadius;

        // 先绘制房间主体矩形（去掉四个角的区域）
        for (int x = room.x; x < right; x++)
        {
            for (int y = room.y; y < top; y++)
            {
                // 跳过四个角的区域，后续用圆角填充
                bool isTopLeftCorner = x < room.x + r && y < room.y + r;
                bool isTopRightCorner = x >= right - r && y < room.y + r;
                bool isBottomLeftCorner = x < room.x + r && y >= top - r;
                bool isBottomRightCorner = x >= right - r && y >= top - r;

                if (!isTopLeftCorner && !isTopRightCorner && !isBottomLeftCorner && !isBottomRightCorner)
                {
                    // 给边缘加轻微噪声扰动，让边缘不是完全笔直，更自然
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * roomEdgeNoise;
                    if (noise < roomEdgeNoise * 0.7f)
                    {
                        grid[x, y] = true;
                    }
                }
            }
        }

        // 绘制四个圆角，实现圆润效果
        DrawCircle(room.x + r, room.y + r, r); // 左上角圆角
        DrawCircle(right - r, room.y + r, r); // 右上角圆角
        DrawCircle(room.x + r, top - r, r); // 左下角圆角
        DrawCircle(right - r, top - r, r); // 右下角圆角
    }

    /// <summary>
    /// 绘制圆形（用于填充房间圆角）
    /// </summary>
    private void DrawCircle(int centerX, int centerY, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                // 计算到圆心的距离，实现圆形
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance <= radius)
                {
                    // 防止超出迷宫边界
                    if (x >= 0 && x < mazeWidth && y >= 0 && y < mazeHeight)
                    {
                        grid[x, y] = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查房间是否重叠
    /// </summary>
    private bool IsRoomOverlap(Room newRoom)
    {
        foreach (Room existingRoom in AllGeneratedRooms)
        {
            bool overlapX = newRoom.x < existingRoom.x + existingRoom.width + roomMinSpacing
                         && newRoom.x + newRoom.width + roomMinSpacing > existingRoom.x;
            bool overlapY = newRoom.y < existingRoom.y + existingRoom.height + roomMinSpacing
                         && newRoom.y + newRoom.height + roomMinSpacing > existingRoom.y;

            if (overlapX && overlapY) return true;
        }
        return false;
    }
    #endregion

    #region 第二步：生成无重复的扭曲道路
    /// <summary>
    /// 用最小生成树生成无重复连接，再用贝塞尔曲线生成扭曲道路
    /// </summary>
    private void ConnectRoomsWithTwistedRoad()
    {
        if (AllGeneratedRooms.Count <= 1) return;

        // 1. 生成所有房间之间的边，按距离排序
        List<MazeEdge> allEdges = new List<MazeEdge>();
        for (int i = 0; i < AllGeneratedRooms.Count; i++)
        {
            for (int j = i + 1; j < AllGeneratedRooms.Count; j++)
            {
                float distance = Vector2Int.Distance(AllGeneratedRooms[i].center, AllGeneratedRooms[j].center);
                allEdges.Add(new MazeEdge() { roomAIndex = i, roomBIndex = j, distance = distance });
            }
        }

        // 2. Kruskal算法生成最小生成树，只保留必要的连接，彻底解决道路重复
        allEdges = allEdges.OrderBy(e => e.distance).ToList();
        int[] parent = Enumerable.Range(0, AllGeneratedRooms.Count).ToArray();
        List<MazeEdge> finalEdges = new List<MazeEdge>();

        int Find(int x)
        {
            if (parent[x] != x) parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(int x, int y)
        {
            parent[Find(x)] = Find(y);
        }

        foreach (var edge in allEdges)
        {
            if (Find(edge.roomAIndex) != Find(edge.roomBIndex))
            {
                Union(edge.roomAIndex, edge.roomBIndex);
                finalEdges.Add(edge);
                if (finalEdges.Count == AllGeneratedRooms.Count - 1) break;
            }
        }

        // 3. 给每条连接生成扭曲的道路
        foreach (var edge in finalEdges)
        {
            Vector2Int start = AllGeneratedRooms[edge.roomAIndex].center;
            Vector2Int end = AllGeneratedRooms[edge.roomBIndex].center;
            DrawTwistedRoad(start, end);
        }
    }

    /// <summary>
    /// 用二次贝塞尔曲线生成扭曲的宽道路（已修复所有编译报错）
    /// </summary>
    private void DrawTwistedRoad(Vector2Int start, Vector2Int end)
    {
        // ========== 修复核心：所有Vector2Int先转Vector2再做浮点运算 ==========
        // 把起点终点转为浮点型向量，支持后续的浮点乘除、归一化运算
        Vector2 startFloat = start;
        Vector2 endFloat = end;

        // 1. 生成贝塞尔曲线的控制点，实现扭曲效果
        // 修复报错1：Vector2Int不能直接除以float，先转Vector2再计算中点
        Vector2 midPoint = (startFloat + endFloat) / 2f;

        // 修复报错2：Vector2Int没有normalized属性，先转Vector2再计算垂直方向+归一化
        Vector2 dir = (endFloat - startFloat).normalized; // 路径的前进方向
        Vector2 normalDir = Vector2.Perpendicular(dir); // 垂直于路径的方向（用于扭曲偏移）

        float randomOffset = Random.Range(-roadTwistIntensity, roadTwistIntensity);
        Vector2 controlPoint = midPoint + normalDir * randomOffset;

        // 2. 沿贝塞尔曲线采样，生成平滑的路径点
        List<Vector2Int> pathPoints = new List<Vector2Int>();
        // 计算采样步长，保证曲线平滑
        float pathLength = Vector2Int.Distance(start, end);
        float step = 1f / (pathLength * roadSmoothStep);

        for (float t = 0; t <= 1; t += step)
        {
            // 修复报错3：float不能直接乘以Vector2Int，全部用浮点型向量计算贝塞尔曲线
            // 二次贝塞尔曲线公式（全浮点运算，无类型冲突）
            Vector2 point = (1 - t) * (1 - t) * startFloat
                            + 2 * (1 - t) * t * controlPoint
                            + t * t * endFloat;

            // 计算完成后再转回整型网格坐标
            pathPoints.Add(new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
        }
        // 补充终点，避免路径遗漏
        pathPoints.Add(end);

        // 3. 沿路径点绘制指定宽度的道路，保证全程宽度≥5格
        int halfRoadWidth = Mathf.FloorToInt(roadWidth / 2f);
        foreach (var point in pathPoints)
        {
            for (int xOffset = -halfRoadWidth; xOffset <= halfRoadWidth; xOffset++)
            {
                for (int yOffset = -halfRoadWidth; yOffset <= halfRoadWidth; yOffset++)
                {
                    int targetX = point.x + xOffset;
                    int targetY = point.y + yOffset;
                    // 防止超出迷宫边界
                    if (targetX >= 0 && targetX < mazeWidth && targetY >= 0 && targetY < mazeHeight)
                    {
                        grid[targetX, targetY] = true;
                    }
                }
            }
        }
    }
    #endregion
}