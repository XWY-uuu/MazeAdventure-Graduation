using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("基础属性")]
    public float baseMaxHealth = 100f;
    public float currentHealth;
    public float maxHealth;

    [Header("战斗属性（由装备系统实时更新）")]
    public float currentDamage;
    public float currentAttackSpeed;
    public float currentCriticalRate;
    public float currentCriticalMultiplier;
    public float currentDefense;

    // 生命变更事件
    public delegate void OnHealthChanged(float current, float max);
    public OnHealthChanged onHealthChanged;
    public delegate void OnDeath();
    public OnDeath onDeath;

    private EquipmentSystem _equipmentSystem;

    private void Awake()
    {
        _equipmentSystem = GetComponent<EquipmentSystem>();
        // 初始化生命值
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
    }

    // 受到伤害
    public void TakeDamage(float incomingDamage)
    {
        if (currentHealth <= 0) return;

        // 计算最终伤害（含防御减免）
        float finalDamage = _equipmentSystem != null
            ? _equipmentSystem.GetFinalHurtDamage(incomingDamage)
            : incomingDamage;

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"玩家受到{finalDamage:F1}点伤害，当前血量：{currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 治疗
    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"玩家恢复{healAmount:F1}点血量，当前血量：{currentHealth:F1}/{maxHealth:F1}");
    }

    // 死亡逻辑
    private void Die()
    {
        Debug.Log("玩家死亡！");
        onDeath?.Invoke();
        // 可扩展死亡逻辑：游戏结束、复活界面等
    }
}