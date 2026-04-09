// 玩家属性类（挂载在玩家对象上）
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("攻击属性")]
    public float attackPower = 50f;       // 基础攻击力
    public float attackRange = 2f;        // 攻击范围
    public float attackCooldown = 1f;     // 攻击冷却时间
    public float criticalRate = 0.2f;     // 暴击率（20%）
    public float criticalDamageMulti = 2f;// 暴击伤害倍率
    public ElementType attackElement = ElementType.Fire; // 攻击属性

    // 对外暴露冷却剩余时间（供攻击逻辑判断）
    [HideInInspector] public float currentAttackCooldown;

    private void Update()
    {
        // 冷却时间倒计时
        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }
    }

    // 属性枚举（用于属性克制）
    public enum ElementType
    {
        Fire, Water, Wood, None
    }
}

// 敌人属性类（挂载在敌人对象上）
public class EnemyStats : MonoBehaviour
{
    [Header("防御属性")]
    public float maxHp = 200f;
    public float currentHp;
    public float defense = 10f;          // 基础防御力
    public PlayerStats.ElementType enemyElement = PlayerStats.ElementType.Wood;

    private void Start()
    {
        currentHp = maxHp;
    }

    // 敌人扣血方法
    public void TakeDamage(float damage)
    {
        currentHp = Mathf.Max(currentHp - damage, 0);
        Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，剩余血量：{currentHp}");

        // 血量为0时触发死亡逻辑
        if (currentHp <= 0)
        {
            OnDeath();
        }
    }

    // 死亡逻辑（可扩展：播放死亡动画、掉落道具等）
    private void OnDeath()
    {
        Debug.Log($"{gameObject.name} 已死亡");
        Destroy(gameObject, 1f); // 延迟销毁
    }
}