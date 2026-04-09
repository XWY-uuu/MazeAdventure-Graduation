using UnityEngine;

public class ResourcePoint : MonoBehaviour
{
    [Header("资源点配置")]
    [Tooltip("交互距离")] public float interactRange = 3f;
    [Tooltip("刷新冷却时间（秒）")] public float refreshCooldown = 60f;
    [Tooltip("是否可重复使用")] public bool canReuse = true;
    [Tooltip("单次生成道具数量")] public int itemCountPerUse = 1;

    [Header("状态")]
    public bool isUsable = true;
    private float _cooldownTimer;
    private GameObject _player;
    private EquipmentSystem _playerEquipmentSystem;

    [Header("可视化")]
    [SerializeField] private GameObject usableEffect;
    [SerializeField] private GameObject cooldownEffect;

    private void Awake()
    {
        // 自动查找玩家
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _playerEquipmentSystem = _player.GetComponent<EquipmentSystem>();
        }

        // 初始化状态
        UpdateResourceState();
    }

    private void Update()
    {
        // 冷却计时
        if (!isUsable && canReuse)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0)
            {
                isUsable = true;
                UpdateResourceState();
                Debug.Log("资源点已刷新，可再次交互！");
            }
        }

        // 玩家交互检测
        if (isUsable && _player != null && _playerEquipmentSystem != null)
        {
            float distance = Vector3.Distance(transform.position, _player.transform.position);
            if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
            {
                UseResourcePoint();
            }
        }
    }

    // 使用资源点，生成道具
    private void UseResourcePoint()
    {
        if (!isUsable) return;

        // 生成随机道具
        for (int i = 0; i < itemCountPerUse; i++)
        {
            ItemData randomItem = ItemGenerator.Instance.GenerateRandomItem();
            if (randomItem != null)
            {
                _playerEquipmentSystem.AddItemToInventory(randomItem);
            }
        }

        // 更新资源点状态
        isUsable = false;
        if (canReuse)
        {
            _cooldownTimer = refreshCooldown;
        }

        UpdateResourceState();
    }

    // 更新资源点视觉状态
    private void UpdateResourceState()
    {
        if (usableEffect != null)
        {
            usableEffect.SetActive(isUsable);
        }
        if (cooldownEffect != null)
        {
            cooldownEffect.SetActive(!isUsable);
        }
    }

    // 场景内绘制交互范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}