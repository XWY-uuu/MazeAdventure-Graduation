using UnityEngine;
using System.Collections.Generic;

// 行为树节点状态
public enum NodeState
{
    Running,
    Success,
    Failure
}

// 行为树基础节点抽象类
public abstract class BaseNode
{
    protected NodeState _state;
    public NodeState State => _state;
    // 节点所属行为树（用于共享黑板数据）
    protected BehaviorTree _tree;
    // 黑板：存储行为树共享数据（如玩家位置、敌人状态等）
    public Blackboard Blackboard => _tree.Blackboard;

    public BaseNode(BehaviorTree tree)
    {
        _tree = tree;
    }

    // 执行节点逻辑
    public abstract NodeState Evaluate();
}

// 组合节点：选择器（Selector）- 只要有一个子节点成功则成功，全部失败才失败
public class SelectorNode : BaseNode
{
    protected BaseNode[] _children;

    public SelectorNode(BehaviorTree tree, params BaseNode[] children) : base(tree)
    {
        _children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (var child in _children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Running:
                    _state = NodeState.Running;
                    return _state;
                case NodeState.Success:
                    _state = NodeState.Success;
                    return _state;
                case NodeState.Failure:
                    continue;
                default:
                    continue;
            }
        }
        _state = NodeState.Failure;
        return _state;
    }
}

// 组合节点：序列器（Sequence）- 所有子节点成功才成功，一个失败则失败
public class SequenceNode : BaseNode
{
    protected BaseNode[] _children;

    public SequenceNode(BehaviorTree tree, params BaseNode[] children) : base(tree)
    {
        _children = children;
    }

    public override NodeState Evaluate()
    {
        bool isAnyChildRunning = false;
        foreach (var child in _children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Running:
                    isAnyChildRunning = true;
                    continue;
                case NodeState.Success:
                    continue;
                case NodeState.Failure:
                    _state = NodeState.Failure;
                    return _state;
                default:
                    _state = NodeState.Failure;
                    return _state;
            }
        }

        _state = isAnyChildRunning ? NodeState.Running : NodeState.Success;
        return _state;
    }
}

// 条件节点抽象类（返回Success/Failure，无Running）
public abstract class ConditionNode : BaseNode
{
    public ConditionNode(BehaviorTree tree) : base(tree) { }
    public override abstract NodeState Evaluate();
}

// 行为节点抽象类（可返回Running/Success/Failure）
public abstract class ActionNode : BaseNode
{
    public ActionNode(BehaviorTree tree) : base(tree) { }
    public override abstract NodeState Evaluate();
}

// 黑板类：存储行为树共享数据
[System.Serializable]
public class Blackboard
{
    // 核心共享数据
    public GameObject self; // 敌人自身
    public GameObject player; // 玩家对象
    public Vector3 targetPosition; // 目标位置（巡逻/追击）
    public float health; // 敌人血量
    public float healthThreshold = 30f; // 躲避行为触发阈值
    public float viewRange = 15f; // 视野范围
    public float attackRange = 2f; // 攻击范围
    public float moveSpeed = 5f; // 移动速度
    public float fleeSpeed = 7f; // 躲避速度
    public float gravity = 9.81f; // ========== 修复：新增重力参数 ==========
    public bool isPlayerInSight; // 是否发现玩家
    public bool isAttacking; // 是否正在攻击
    public List<Vector2Int> patrolPath; // 巡逻路径
    public int currentPatrolPointIndex; // 当前巡逻点索引
    public float baseDamage = 20f; // 敌人基础伤害
}