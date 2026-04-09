using UnityEngine;
using System.Collections.Generic;

public class EnemyBehaviorTree : BehaviorTree
{
    private JPSOptimization _pathfinder;
    private CharacterController _characterController;
    private Vector3 _currentMoveDir;
    private LayerMask _enemyLayerMask; // 敌人自身的层，射线忽略
    private Color _gizmosColor = Color.green;

    // ========== 修复：正确重写父类的virtual Awake方法 ==========
    protected override void Awake()
    {
        // 提前初始化层，确保射线检测正常
        _enemyLayerMask = ~LayerMask.GetMask("Enemy"); // 射线忽略Enemy层
        base.Awake(); // 调用父类Awake，执行黑板初始化、行为树构建
    }

    protected override BaseNode ConstructTree()
    {
        // 初始化组件
        _pathfinder = GetComponent<JPSOptimization>();
        _characterController = GetComponent<CharacterController>();

        // 自动添加CharacterController
        if (_characterController == null)
        {
            _characterController = gameObject.AddComponent<CharacterController>();
            _characterController.radius = 0.4f;
            _characterController.height = 2f;
            _characterController.skinWidth = 0.01f;
            _characterController.center = new Vector3(0, 1f, 0);
            _characterController.stepOffset = 0.3f;
            _characterController.enableOverlapRecovery = true;
        }

        // 寻路脚本检查
        if (_pathfinder == null)
        {
            Debug.LogError($"敌人{gameObject.name}缺少JPSOptimization脚本！");
            return null;
        }

        // 刷新寻路网格
        _pathfinder.RefreshGrid();

        // 强制初始化Blackboard
        if (Blackboard == null)
        {
            Blackboard = new Blackboard();
            Blackboard.self = gameObject;
            Blackboard.gravity = 9.81f;
            Blackboard.health = 100f;
        }
        Blackboard.self = gameObject;

        // 查找玩家（兜底）
        if (Blackboard.player == null)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.CompareTag("Player"))
                {
                    Blackboard.player = obj;
                    break;
                }
            }
        }

        if (Blackboard.player == null)
        {
            Debug.LogError($"敌人{gameObject.name}找不到玩家！请确保玩家Tag为Player");
        }

        // 初始化巡逻路径
        Blackboard.patrolPath = GeneratePatrolPath();
        Blackboard.currentPatrolPointIndex = 0;

        // 构建行为树
        var root = new SelectorNode(this,
            new SequenceNode(this,
                new Condition_IsHealthLow(this),
                new Action_FleeFromPlayer(this)
            ),
            new SequenceNode(this,
                new Condition_IsPlayerInSight(this),
                new Condition_IsPlayerInAttackRange(this),
                new Action_AttackPlayer(this)
            ),
            new SequenceNode(this,
                new Condition_IsPlayerInSight(this),
                new Action_ChasePlayer(this)
            ),
            new Action_Patrol(this)
        );

        return root;
    }

    // 生成巡逻路径
    private List<Vector2Int> GeneratePatrolPath()
    {
        BlockMazeGenerator mazeGenerator = Object.FindFirstObjectByType<BlockMazeGenerator>();
        if (mazeGenerator == null || mazeGenerator.AllGeneratedRooms.Count == 0)
        {
            Vector2Int currentPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
            return new List<Vector2Int>() {
                currentPos,
                currentPos + Vector2Int.right * 5,
                currentPos + Vector2Int.up * 5
            };
        }

        Vector2Int currentGridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        Room currentRoom = mazeGenerator.AllGeneratedRooms.Find(r =>
            currentGridPos.x >= r.x && currentGridPos.x < r.x + r.width &&
            currentGridPos.y >= r.y && currentGridPos.y < r.y + r.height
        );

        if (currentRoom.Equals(default(Room)))
        {
            return new List<Vector2Int>() { currentGridPos, currentGridPos + Vector2Int.right * 5, currentGridPos + Vector2Int.up * 5 };
        }

        List<Vector2Int> patrolPoints = new List<Vector2Int>();
        List<Vector2Int> roomGround = mazeGenerator.GetGroundPositionsInRoom(currentRoom);
        int pointCount = Random.Range(3, 6);

        for (int i = 0; i < pointCount; i++)
        {
            if (roomGround.Count == 0) break;
            int randomIdx = Random.Range(0, roomGround.Count);
            patrolPoints.Add(roomGround[randomIdx]);
            roomGround.RemoveAt(randomIdx);
        }

        return patrolPoints;
    }

    // 通用移动方法
    private void MoveToTarget(Vector3 target, float speed)
    {
        if (_characterController == null) return;

        _currentMoveDir = (target - transform.position).normalized;
        _currentMoveDir.y = 0;

        // 重力处理
        if (_characterController.isGrounded)
        {
            _currentMoveDir.y = -1f;
        }
        else
        {
            _currentMoveDir.y -= Blackboard.gravity * Time.deltaTime;
        }

        // 执行移动
        _characterController.Move(_currentMoveDir * speed * Time.deltaTime);

        // 旋转修复：只绕Y轴
        if (_currentMoveDir.magnitude > 0.1f)
        {
            Vector3 lookDir = new Vector3(_currentMoveDir.x, 0, _currentMoveDir.z);
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    // 攻击状态重置
    public void ResetAttackState()
    {
        if (Blackboard != null)
            Blackboard.isAttacking = false;
    }

    // 可视化调试
    private void OnDrawGizmosSelected()
    {
        if (Blackboard == null) return;

        // 视野范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, Blackboard.viewRange);

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, Blackboard.attackRange);

        // 巡逻路径
        if (Blackboard.patrolPath != null && Blackboard.patrolPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < Blackboard.patrolPath.Count; i++)
            {
                Vector3 pointWorld = new Vector3(Blackboard.patrolPath[i].x, 0.5f, Blackboard.patrolPath[i].y);
                Gizmos.DrawSphere(pointWorld, 0.3f);
                if (i < Blackboard.patrolPath.Count - 1)
                {
                    Vector3 nextPoint = new Vector3(Blackboard.patrolPath[i + 1].x, 0.5f, Blackboard.patrolPath[i + 1].y);
                    Gizmos.DrawLine(pointWorld, nextPoint);
                }
            }
        }

        // 目标线
        if (Blackboard.isPlayerInSight && Blackboard.player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, Blackboard.player.transform.position + Vector3.up);
            _gizmosColor = Color.red;
        }
        else
        {
            if (Blackboard.patrolPath != null && Blackboard.patrolPath.Count > 0)
            {
                Vector3 targetPoint = new Vector3(Blackboard.patrolPath[Blackboard.currentPatrolPointIndex].x, 0.5f, Blackboard.patrolPath[Blackboard.currentPatrolPointIndex].y);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + Vector3.up, targetPoint);
                _gizmosColor = Color.green;
            }
        }

        // 状态标识
        Gizmos.color = _gizmosColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2.2f, new Vector3(1f, 0.2f, 1f));
    }

    // ========== 条件节点 ==========
    private class Condition_IsHealthLow : ConditionNode
    {
        public Condition_IsHealthLow(BehaviorTree tree) : base(tree) { }

        public override NodeState Evaluate()
        {
            if (Blackboard == null)
            {
                _state = NodeState.Failure;
                return _state;
            }
            _state = Blackboard.health < Blackboard.healthThreshold ? NodeState.Success : NodeState.Failure;
            return _state;
        }
    }

    // 视野检测核心逻辑
    private class Condition_IsPlayerInSight : ConditionNode
    {
        private EnemyBehaviorTree _enemyTree;
        public Condition_IsPlayerInSight(EnemyBehaviorTree tree) : base(tree)
        {
            _enemyTree = tree;
        }

        public override NodeState Evaluate()
        {
            // 空保护
            if (Blackboard == null || Blackboard.self == null || Blackboard.player == null)
            {
                // 重新找玩家
                if (Blackboard != null && Blackboard.player == null)
                {
                    GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (var obj in allObjects)
                    {
                        if (obj.CompareTag("Player"))
                        {
                            Blackboard.player = obj;
                            Debug.Log($"敌人{Blackboard.self.name}重新找到玩家！");
                            break;
                        }
                    }
                }

                if (Blackboard != null)
                    Blackboard.isPlayerInSight = false;
                _state = NodeState.Failure;
                return _state;
            }

            // 距离检查
            float distance = Vector3.Distance(Blackboard.self.transform.position, Blackboard.player.transform.position);
            if (distance > Blackboard.viewRange)
            {
                Blackboard.isPlayerInSight = false;
                _state = NodeState.Failure;
                return _state;
            }

            // 射线检测：忽略Enemy层，不会被自己挡住
            Vector3 rayStart = Blackboard.self.transform.position + Vector3.up * 1f;
            Vector3 rayEnd = Blackboard.player.transform.position + Vector3.up * 1f;
            Vector3 dir = (rayEnd - rayStart).normalized;

            bool hitSomething = Physics.Raycast(
                rayStart,
                dir,
                out RaycastHit hit,
                Blackboard.viewRange,
                _enemyTree._enemyLayerMask,
                QueryTriggerInteraction.Ignore
            );

            Blackboard.isPlayerInSight = hitSomething && hit.collider.CompareTag("Player");

            if (Blackboard.isPlayerInSight)
            {
                Debug.Log($"敌人{Blackboard.self.name}看到玩家了！距离：{distance:F2}");
            }

            _state = Blackboard.isPlayerInSight ? NodeState.Success : NodeState.Failure;
            return _state;
        }
    }

    private class Condition_IsPlayerInAttackRange : ConditionNode
    {
        public Condition_IsPlayerInAttackRange(BehaviorTree tree) : base(tree) { }

        public override NodeState Evaluate()
        {
            if (Blackboard == null || Blackboard.player == null)
            {
                _state = NodeState.Failure;
                return _state;
            }

            float distance = Vector3.Distance(Blackboard.self.transform.position, Blackboard.player.transform.position);
            _state = distance <= Blackboard.attackRange ? NodeState.Success : NodeState.Failure;
            return _state;
        }
    }

    // ========== 行为节点 ==========
    private class Action_Patrol : ActionNode
    {
        public Action_Patrol(BehaviorTree tree) : base(tree) { }

        public override NodeState Evaluate()
        {
            EnemyBehaviorTree enemyTree = (EnemyBehaviorTree)_tree;
            Blackboard bb = Blackboard;

            if (bb == null || bb.patrolPath == null || bb.patrolPath.Count == 0)
            {
                _state = NodeState.Failure;
                return _state;
            }

            Vector2Int currentPatrolGrid = bb.patrolPath[bb.currentPatrolPointIndex];
            Vector3 currentPatrolWorld = new Vector3(currentPatrolGrid.x, 0, currentPatrolGrid.y);
            float distance = Vector3.Distance(bb.self.transform.position, currentPatrolWorld);

            if (distance < 1f)
            {
                bb.currentPatrolPointIndex = (bb.currentPatrolPointIndex + 1) % bb.patrolPath.Count;
            }

            enemyTree.MoveToTarget(currentPatrolWorld, bb.moveSpeed);
            _state = NodeState.Running;
            return _state;
        }
    }

    private class Action_ChasePlayer : ActionNode
    {
        public Action_ChasePlayer(BehaviorTree tree) : base(tree) { }

        public override NodeState Evaluate()
        {
            EnemyBehaviorTree enemyTree = (EnemyBehaviorTree)_tree;
            Blackboard bb = Blackboard;

            if (bb == null || bb.player == null || enemyTree._pathfinder == null || !enemyTree._pathfinder.IsGridValid)
            {
                _state = NodeState.Failure;
                return _state;
            }

            Vector2Int playerGrid = new Vector2Int(Mathf.RoundToInt(bb.player.transform.position.x), Mathf.RoundToInt(bb.player.transform.position.z));
            Vector2Int selfGrid = new Vector2Int(Mathf.RoundToInt(bb.self.transform.position.x), Mathf.RoundToInt(bb.self.transform.position.z));

            List<Vector2Int> path = enemyTree._pathfinder.FindPath(selfGrid, playerGrid);
            if (path == null || path.Count == 0)
            {
                // 寻路失败，直接朝玩家直线移动
                enemyTree.MoveToTarget(bb.player.transform.position, bb.moveSpeed);
                _state = NodeState.Running;
                return _state;
            }

            Vector3 targetWorld = new Vector3(path[0].x, 0, path[0].y);
            enemyTree.MoveToTarget(targetWorld, bb.moveSpeed);

            _state = NodeState.Running;
            return _state;
        }
    }

    private class Action_AttackPlayer : ActionNode
    {
        private float _attackCooldown = 1f;
        private float _lastAttackTime;
        private EnemyBehaviorTree _enemyTree;

        public Action_AttackPlayer(EnemyBehaviorTree tree) : base(tree)
        {
            _enemyTree = tree;
        }

        public override NodeState Evaluate()
        {
            Blackboard bb = Blackboard;
            if (bb == null || bb.player == null || bb.isAttacking)
            {
                _state = NodeState.Running;
                return _state;
            }
            if (Time.time - _lastAttackTime < _attackCooldown)
            {
                _state = NodeState.Running;
                return _state;
            }

            // 核心：真实伤害逻辑
            bb.isAttacking = true;
            HealthSystem playerHealth = bb.player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // 可在Blackboard中配置敌人基础伤害，这里默认20点基础伤害
                float enemyDamage = 20f;
                playerHealth.TakeDamage(enemyDamage);
            }

            Debug.Log($"【攻击】{bb.self.name} 攻击玩家！");
            _lastAttackTime = Time.time;
            _enemyTree.Invoke(nameof(_enemyTree.ResetAttackState), 0.5f);
            _state = NodeState.Success;
            return _state;
        }
    }

    private class Action_FleeFromPlayer : ActionNode
    {
        public Action_FleeFromPlayer(BehaviorTree tree) : base(tree) { }

        public override NodeState Evaluate()
        {
            EnemyBehaviorTree enemyTree = (EnemyBehaviorTree)_tree;
            Blackboard bb = Blackboard;

            if (bb == null || bb.player == null)
            {
                _state = NodeState.Failure;
                return _state;
            }

            Vector3 fleeDir = (bb.self.transform.position - bb.player.transform.position).normalized;
            Vector3 fleeTarget = bb.self.transform.position + fleeDir * 5f;

            enemyTree.MoveToTarget(fleeTarget, bb.fleeSpeed);
            _state = NodeState.Running;
            return _state;
        }
    }
}