using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    protected BlockMazeGenerator _mazeGenerator;
    protected bool[,] _grid;
    protected int _width;
    protected int _height;

    private void Awake()
    {
        // 替换为新API
        _mazeGenerator = Object.FindFirstObjectByType<BlockMazeGenerator>();
        if (_mazeGenerator == null)
        {
            Debug.LogError("未找到BlockMazeGenerator组件！");
            return;
        }
        RefreshGrid();
    }

    // 刷新网格数据（迷宫重新生成后调用）
    public void RefreshGrid()
    {
        if (_mazeGenerator == null) return;
        _grid = _mazeGenerator.GetGrid();
        _width = _mazeGenerator.mazeWidth;
        _height = _mazeGenerator.mazeHeight;
    }

    // A*核心算法：输入起点/终点网格坐标，返回路径
    public virtual List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // 边界检查
        if (!IsValidPosition(start) || !IsValidPosition(end) || start == end)
        {
            return new List<Vector2Int>();
        }

        // 开放列表（待检查节点）、关闭列表（已检查节点）
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        // 起点节点
        Node startNode = new Node(start, null, 0, GetHeuristic(start, end));
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // 取出F值最小的节点（F=G+H）
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].F < currentNode.F || (openList[i].F == currentNode.F && openList[i].H < currentNode.H))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // 到达终点，回溯路径
            if (currentNode.Position == end)
            {
                return RetracePath(startNode, currentNode);
            }

            // 遍历相邻节点（4方向，适配迷宫）
            foreach (Vector2Int neighborPos in GetNeighbors(currentNode.Position))
            {
                if (!IsValidPosition(neighborPos) || closedList.Contains(new Node(neighborPos)))
                {
                    continue;
                }

                float newCostToNeighbor = currentNode.G + GetHeuristic(currentNode.Position, neighborPos);
                Node neighborNode = new Node(neighborPos, currentNode, newCostToNeighbor, GetHeuristic(neighborPos, end));

                if (!openList.Contains(neighborNode) || newCostToNeighbor < neighborNode.G)
                {
                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        // 无路径返回空
        return new List<Vector2Int>();
    }

    // 启发函数：曼哈顿距离（适配网格）
    protected float GetHeuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // 回溯路径
    protected List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    // 获取相邻节点（4方向：上下左右）
    protected List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsValidPosition(newPos))
            {
                neighbors.Add(newPos);
            }
        }

        return neighbors;
    }

    // 检查位置是否合法（在网格内+是地面）
    protected bool IsValidPosition(Vector2Int pos)
    {
        if (_grid == null) return false;
        return pos.x >= 0 && pos.x < _width && pos.y >= 0 && pos.y < _height && _grid[pos.x, pos.y];
    }

    // A*节点类
    [System.Serializable]
    public class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public float G; // 起点到当前节点代价
        public float H; // 当前节点到终点预估代价
        public float F => G + H; // 总代价

        public Node(Vector2Int position, Node parent = null, float g = 0, float h = 0)
        {
            Position = position;
            Parent = parent;
            G = g;
            H = h;
        }

        // 重写Equals和GetHashCode，用于HashSet比较
        public override bool Equals(object obj)
        {
            return obj is Node node && Position.Equals(node.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}