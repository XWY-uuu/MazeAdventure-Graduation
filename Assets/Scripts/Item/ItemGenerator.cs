using UnityEngine;
using System.Collections.Generic;

// 道具类型枚举
public enum ItemType
{
    MeleeWeapon,    // 近战武器
    RangedWeapon,   // 远程武器
    Armor           // 护甲
}

// 道具品质枚举（影响属性倍率与随机范围）
public enum ItemQuality
{
    Common,     // 白色 普通
    Uncommon,   // 绿色 优秀
    Rare,       // 蓝色 稀有
    Epic,       // 紫色 史诗
    Legendary   // 橙色 传说
}

// 道具基础数据类
[System.Serializable]
public class ItemData
{
    public string itemName;          // 道具名称
    public ItemType itemType;        // 道具类型
    public ItemQuality itemQuality;  // 道具品质
    public Sprite itemIcon;          // 道具图标（可选，UI用）

    // 武器通用属性
    public float baseDamage;         // 基础伤害
    public float attackSpeed;        // 攻击速度（次/秒）
    public float criticalRate;       // 暴击率（0-1）
    public float criticalMultiplier; // 暴击倍率（默认1.5倍）

    // 护甲专属属性
    public float defense;            // 防御值（减免伤害）
    public float maxHealthBonus;     // 最大生命值加成

    // 移动速度加成（全类型通用）
    public float moveSpeedBonus;     // 移动速度百分比加成

    // 生成道具品质对应的名称前缀
    public string GetQualityPrefix()
    {
        return itemQuality switch
        {
            ItemQuality.Common => "普通的",
            ItemQuality.Uncommon => "优秀的",
            ItemQuality.Rare => "精良的",
            ItemQuality.Epic => "史诗的",
            ItemQuality.Legendary => "传说的",
            _ => ""
        };
    }

    // 生成完整道具名称
    public string GetFullItemName()
    {
        string typeSuffix = itemType switch
        {
            ItemType.MeleeWeapon => "长剑",
            ItemType.RangedWeapon => "火枪",
            ItemType.Armor => "护甲",
            _ => ""
        };
        return $"{GetQualityPrefix()}{typeSuffix}";
    }
}

public class ItemGenerator : MonoBehaviour
{
    // 单例模式，全局调用
    public static ItemGenerator Instance { get; private set; }

    [Header("品质权重配置（数值越大，生成概率越高）")]
    [SerializeField] private int commonWeight = 50;
    [SerializeField] private int uncommonWeight = 30;
    [SerializeField] private int rareWeight = 12;
    [SerializeField] private int epicWeight = 6;
    [SerializeField] private int legendaryWeight = 2;

    [Header("属性基础配置")]
    [Tooltip("近战武器基础伤害区间")] public Vector2 meleeDamageBase = new Vector2(5, 15);
    [Tooltip("远程武器基础伤害区间")] public Vector2 rangedDamageBase = new Vector2(8, 20);
    [Tooltip("护甲基础防御区间")] public Vector2 armorDefenseBase = new Vector2(2, 10);
    [Tooltip("护甲基础生命加成区间")] public Vector2 healthBonusBase = new Vector2(10, 50);

    // 品质对应的属性倍率
    private readonly Dictionary<ItemQuality, float> _qualityMultiplier = new Dictionary<ItemQuality, float>()
    {
        { ItemQuality.Common, 1f },
        { ItemQuality.Uncommon, 1.5f },
        { ItemQuality.Rare, 2.2f },
        { ItemQuality.Epic, 3.2f },
        { ItemQuality.Legendary, 5f }
    };

    private List<ItemQuality> _weightedQualityList;
    private int _totalWeight;

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨关卡保留
        }

        // 初始化品质权重表
        InitWeightedQualityList();
    }

    // 初始化权重表
    private void InitWeightedQualityList()
    {
        _weightedQualityList = new List<ItemQuality>();
        _totalWeight = 0;

        AddQualityToWeightedList(ItemQuality.Common, commonWeight);
        AddQualityToWeightedList(ItemQuality.Uncommon, uncommonWeight);
        AddQualityToWeightedList(ItemQuality.Rare, rareWeight);
        AddQualityToWeightedList(ItemQuality.Epic, epicWeight);
        AddQualityToWeightedList(ItemQuality.Legendary, legendaryWeight);
    }

    private void AddQualityToWeightedList(ItemQuality quality, int weight)
    {
        for (int i = 0; i < weight; i++)
        {
            _weightedQualityList.Add(quality);
        }
        _totalWeight += weight;
    }

    // 核心：随机生成一个道具
    public ItemData GenerateRandomItem(ItemType? forcedType = null)
    {
        ItemData newItem = new ItemData();

        // 1. 确定道具类型（强制类型/完全随机）
        if (forcedType.HasValue)
        {
            newItem.itemType = forcedType.Value;
        }
        else
        {
            System.Array itemTypes = System.Enum.GetValues(typeof(ItemType));
            newItem.itemType = (ItemType)itemTypes.GetValue(Random.Range(0, itemTypes.Length));
        }

        // 2. 随机生成品质
        newItem.itemQuality = _weightedQualityList[Random.Range(0, _totalWeight)];
        float multiplier = _qualityMultiplier[newItem.itemQuality];

        // 3. 根据类型生成对应属性
        switch (newItem.itemType)
        {
            case ItemType.MeleeWeapon:
                GenerateMeleeWeaponAttributes(newItem, multiplier);
                break;
            case ItemType.RangedWeapon:
                GenerateRangedWeaponAttributes(newItem, multiplier);
                break;
            case ItemType.Armor:
                GenerateArmorAttributes(newItem, multiplier);
                break;
        }

        // 4. 生成道具名称
        newItem.itemName = newItem.GetFullItemName();

        Debug.Log($"生成道具：{newItem.itemName}，品质：{newItem.itemQuality}");
        return newItem;
    }

    // 生成近战武器属性
    private void GenerateMeleeWeaponAttributes(ItemData item, float multiplier)
    {
        item.baseDamage = Random.Range(meleeDamageBase.x, meleeDamageBase.y) * multiplier;
        item.attackSpeed = Mathf.Clamp(Random.Range(0.8f, 1.5f) * multiplier, 0.5f, 4f);
        item.criticalRate = Mathf.Clamp(Random.Range(0.02f, 0.1f) * multiplier, 0.01f, 0.5f);
        item.criticalMultiplier = 1.5f + (multiplier * 0.2f);
        item.moveSpeedBonus = Random.Range(-0.05f, 0.05f) * multiplier;
    }

    // 生成远程武器属性
    private void GenerateRangedWeaponAttributes(ItemData item, float multiplier)
    {
        item.baseDamage = Random.Range(rangedDamageBase.x, rangedDamageBase.y) * multiplier;
        item.attackSpeed = Mathf.Clamp(Random.Range(0.5f, 1.2f) * multiplier, 0.3f, 3f);
        item.criticalRate = Mathf.Clamp(Random.Range(0.03f, 0.12f) * multiplier, 0.01f, 0.6f);
        item.criticalMultiplier = 1.5f + (multiplier * 0.3f);
        item.moveSpeedBonus = Random.Range(-0.08f, 0.03f) * multiplier;
    }

    // 生成护甲属性
    private void GenerateArmorAttributes(ItemData item, float multiplier)
    {
        item.defense = Random.Range(armorDefenseBase.x, armorDefenseBase.y) * multiplier;
        item.maxHealthBonus = Random.Range(healthBonusBase.x, healthBonusBase.y) * multiplier;
        item.moveSpeedBonus = Random.Range(-0.03f, 0.08f) * multiplier;
        // 护甲无攻击属性，默认赋值0
        item.baseDamage = 0;
        item.attackSpeed = 0;
        item.criticalRate = 0;
        item.criticalMultiplier = 1f;
    }
}