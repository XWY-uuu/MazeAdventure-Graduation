using UnityEngine;

public abstract class BehaviorTree : MonoBehaviour
{
    public Blackboard Blackboard;
    private BaseNode _rootNode;

    // ========== 修复：把private改为protected virtual，支持子类重写 ==========
    protected virtual void Awake()
    {
        // 初始化黑板
        if (Blackboard == null)
        {
            Blackboard = new Blackboard();
            Blackboard.self = gameObject;
            Blackboard.gravity = 9.81f;
            Blackboard.health = 100f;
        }

        // 查找玩家
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.CompareTag("Player"))
            {
                Blackboard.player = obj;
                break;
            }
        }

        if (Blackboard.player == null)
        {
            Debug.LogError($"行为树{gameObject.name}找不到玩家！请确保玩家对象Tag为Player");
        }

        // 构建行为树
        _rootNode = ConstructTree();
    }

    protected virtual void Update()
    {
        if (_rootNode != null && Blackboard != null && Blackboard.health > 0)
        {
            _rootNode.Evaluate();
        }
    }

    protected abstract BaseNode ConstructTree();
}