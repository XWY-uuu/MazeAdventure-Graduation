using UnityEngine;
using System.Collections.Generic;

public class JPSOptimization : AStarPathfinding
{
    // 新增：对外暴露网格是否有效
    public bool IsGridValid => _grid != null;

    // JPS核心：跳跃点搜索，减少A*的节点检查数量
    public new List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // 新增：空网格直接返回空路径，防止报错
        if (!IsGridValid || !IsValidPosition(start) || !IsValidPosition(end) || start == end)
        {
            return new List<Vector2Int>();
        }

        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        Node startNode = new Node(start, null, 0, GetHeuristic(start, end));
        openList.Add(startNode);

        while (openList.Count > 0)
        {
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

            if (currentNode.Position == end)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (var jumpPoint in FindJumpPoints(currentNode.Position, end))
            {
                if (closedList.Contains(new Node(jumpPoint))) continue;

                float cost = currentNode.G + GetHeuristic(currentNode.Position, jumpPoint);
                Node jumpNode = new Node(jumpPoint, currentNode, cost, GetHeuristic(jumpPoint, end));

                if (!openList.Contains(jumpNode) || cost < jumpNode.G)
                {
                    if (!openList.Contains(jumpNode))
                    {
                        openList.Add(jumpNode);
                    }
                }
            }
        }

        return new List<Vector2Int>();
    }

    // 查找跳跃点
    private List<Vector2Int> FindJumpPoints(Vector2Int current, Vector2Int goal)
    {
        List<Vector2Int> jumpPoints = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int jumpPoint = Jump(current, dir, goal);
            if (jumpPoint != Vector2Int.zero && IsValidPosition(jumpPoint))
            {
                jumpPoints.Add(jumpPoint);
            }
        }

        return jumpPoints;
    }

    // 跳跃逻辑
    private Vector2Int Jump(Vector2Int current, Vector2Int direction, Vector2Int goal)
    {
        Vector2Int next = current + direction;

        if (!IsValidPosition(next))
        {
            return Vector2Int.zero;
        }

        if (next == goal)
        {
            return next;
        }

        if (HasForcedNeighbor(next, direction))
        {
            return next;
        }

        return Jump(next, direction, goal);
    }

    // 检查是否有强制邻居
    private bool HasForcedNeighbor(Vector2Int pos, Vector2Int direction)
    {
        if (direction.x != 0)
        {
            if (!IsValidPosition(pos + Vector2Int.up) && IsValidPosition(pos + Vector2Int.up + direction))
                return true;
            if (!IsValidPosition(pos + Vector2Int.down) && IsValidPosition(pos + Vector2Int.down + direction))
                return true;
        }
        else if (direction.y != 0)
        {
            if (!IsValidPosition(pos + Vector2Int.left) && IsValidPosition(pos + Vector2Int.left + direction))
                return true;
            if (!IsValidPosition(pos + Vector2Int.right) && IsValidPosition(pos + Vector2Int.right + direction))
                return true;
        }

        return false;
    }
}