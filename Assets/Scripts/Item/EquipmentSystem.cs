using UnityEngine;
using System.Collections.Generic;

public class EquipmentSystem : MonoBehaviour
{
    [Header("装备栏配置")]
    [Tooltip("背包最大容量")] public int maxInventorySize = 20;

    // 当前装备
    public ItemData currentMeleeWeapon { get; private set; }
    public ItemData currentRangedWeapon { get; private set; }
    public ItemData currentArmor { get; private set; }

    // 背包列表
    public List<ItemData> inventory { get; private set; } = new List<ItemData>();

    // 玩家属性事件（UI、战斗系统可监听）
    public delegate void OnAttributeChanged();
    public OnAttributeChanged onAttributeChanged;

    // 玩家组件引用
    private PlayerController _playerController;
    private HealthSystem _playerHealthSystem;

    private void Awake()
    {
        // 获取玩家组件
        _playerController = GetComponent<PlayerController>();
        _playerHealthSystem = GetComponent<HealthSystem>();

        // 跨关卡保留玩家状态
        DontDestroyOnLoad(gameObject);
    }

    #region 背包管理
    // 添加道具到背包
    public bool AddItemToInventory(ItemData item)
    {
        if (inventory.Count >= maxInventorySize)
        {
            Debug.LogWarning("背包已满，无法拾取道具！");
            return false;
        }

        inventory.Add(item);
        Debug.Log($"拾取道具：{item.itemName}，当前背包数量：{inventory.Count}/{maxInventorySize}");
        return true;
    }

    // 从背包移除道具
    public void RemoveItemFromInventory(ItemData item)
    {
        if (inventory.Contains(item))
        {
            inventory.Remove(item);
        }
    }

    // 清空背包（仅重开游戏时调用）
    public void ClearInventory()
    {
        inventory.Clear();
        UnEquipAll();
    }
    #endregion

    #region 装备穿戴与卸下
    // 穿戴装备
    public void EquipItem(ItemData item)
    {
        if (item == null) return;

        // 先卸下同类型装备
        switch (item.itemType)
        {
            case ItemType.MeleeWeapon:
                if (currentMeleeWeapon != null)
                {
                    UnEquipItem(ItemType.MeleeWeapon);
                }
                currentMeleeWeapon = item;
                break;
            case ItemType.RangedWeapon:
                if (currentRangedWeapon != null)
                {
                    UnEquipItem(ItemType.RangedWeapon);
                }
                currentRangedWeapon = item;
                break;
            case ItemType.Armor:
                if (currentArmor != null)
                {
                    UnEquipItem(ItemType.Armor);
                }
                currentArmor = item;
                break;
        }

        // 从背包移除已穿戴的道具
        RemoveItemFromInventory(item);
        // 更新玩家属性
        UpdatePlayerAttributes();
        Debug.Log($"穿戴装备：{item.itemName}");
    }

    // 卸下指定类型装备
    public void UnEquipItem(ItemType itemType)
    {
        ItemData unequippedItem = null;

        switch (itemType)
        {
            case ItemType.MeleeWeapon:
                unequippedItem = currentMeleeWeapon;
                currentMeleeWeapon = null;
                break;
            case ItemType.RangedWeapon:
                unequippedItem = currentRangedWeapon;
                currentRangedWeapon = null;
                break;
            case ItemType.Armor:
                unequippedItem = currentArmor;
                currentArmor = null;
                break;
        }

        // 把卸下的装备放回背包
        if (unequippedItem != null)
        {
            AddItemToInventory(unequippedItem);
        }

        // 更新玩家属性
        UpdatePlayerAttributes();
        Debug.Log($"卸下装备：{unequippedItem?.itemName}");
    }

    // 卸下所有装备
    public void UnEquipAll()
    {
        UnEquipItem(ItemType.MeleeWeapon);
        UnEquipItem(ItemType.RangedWeapon);
        UnEquipItem(ItemType.Armor);
    }
    #endregion

    #region 玩家属性计算与更新
    // 实时更新玩家所有属性
    private void UpdatePlayerAttributes()
    {
        UpdatePlayerMoveSpeed();
        UpdatePlayerHealth();
        UpdatePlayerCombatAttributes();

        // 触发属性变更事件
        onAttributeChanged?.Invoke();
    }

    // 更新移动速度
    private void UpdatePlayerMoveSpeed()
    {
        float baseSpeed = _playerController.baseMoveSpeed;
        float totalBonus = 0f;

        if (currentMeleeWeapon != null) totalBonus += currentMeleeWeapon.moveSpeedBonus;
        if (currentRangedWeapon != null) totalBonus += currentRangedWeapon.moveSpeedBonus;
        if (currentArmor != null) totalBonus += currentArmor.moveSpeedBonus;

        _playerController.currentMoveSpeed = baseSpeed * (1 + totalBonus);
    }

    // 更新生命值
    private void UpdatePlayerHealth()
    {
        float baseMaxHealth = _playerHealthSystem.baseMaxHealth;
        float totalBonus = 0f;

        if (currentArmor != null) totalBonus += currentArmor.maxHealthBonus;

        _playerHealthSystem.maxHealth = baseMaxHealth + totalBonus;
        // 保证当前血量不超过最大血量
        _playerHealthSystem.currentHealth = Mathf.Min(_playerHealthSystem.currentHealth, _playerHealthSystem.maxHealth);
    }

    // 更新战斗属性
    private void UpdatePlayerCombatAttributes()
    {
        // 主武器属性生效（默认近战优先，可根据需求切换）
        ItemData mainWeapon = currentMeleeWeapon ?? currentRangedWeapon;

        _playerHealthSystem.currentDamage = mainWeapon?.baseDamage ?? 5f; // 空手基础伤害5
        _playerHealthSystem.currentAttackSpeed = mainWeapon?.attackSpeed ?? 1f;
        _playerHealthSystem.currentCriticalRate = mainWeapon?.criticalRate ?? 0.05f;
        _playerHealthSystem.currentCriticalMultiplier = mainWeapon?.criticalMultiplier ?? 1.5f;
        _playerHealthSystem.currentDefense = currentArmor?.defense ?? 0f;
    }

    // 获取玩家最终伤害（含暴击计算）
    public float GetFinalDamage()
    {
        float baseDamage = _playerHealthSystem.currentDamage;
        bool isCritical = Random.value <= _playerHealthSystem.currentCriticalRate;

        return isCritical ? baseDamage * _playerHealthSystem.currentCriticalMultiplier : baseDamage;
    }

    // 获取玩家受到的最终伤害（含防御减免）
    public float GetFinalHurtDamage(float incomingDamage)
    {
        float damageReduction = Mathf.Clamp(_playerHealthSystem.currentDefense * 0.05f, 0f, 0.75f); // 最高减免75%伤害
        return Mathf.Max(1f, incomingDamage * (1 - damageReduction)); // 最低受到1点伤害
    }
    #endregion
}