using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float _currentHealth;
    private EnemyBehaviorTree _enemyBT;

    // 【修复】用系统内置Action替代自定义委托，彻底解决命名冲突
    // 敌人死亡事件，其他脚本可以监听这个事件
    public event System.Action OnDeath;

    private void Awake()
    {
        _enemyBT = GetComponent<EnemyBehaviorTree>();
        // 【补充修复】初始化敌人血量，避免初始血量为0的bug
        _currentHealth = maxHealth;

        // 同步行为树黑板血量
        if (_enemyBT != null && _enemyBT.Blackboard != null)
        {
            _enemyBT.Blackboard.health = _currentHealth;
        }
    }

    // 受到伤害
    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0) return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        // 同步到行为树黑板
        if (_enemyBT != null && _enemyBT.Blackboard != null)
        {
            _enemyBT.Blackboard.health = _currentHealth;
        }

        Debug.Log($"{gameObject.name}受到{damage:F1}点伤害，当前血量：{_currentHealth:F1}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    // 死亡逻辑
    private void Die()
    {
        Debug.Log($"{gameObject.name} 已死亡！");
        // 触发死亡事件，通知EnemySpawner计数
        OnDeath?.Invoke();
        Destroy(gameObject, 0.2f);
    }
}