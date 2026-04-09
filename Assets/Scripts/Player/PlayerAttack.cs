using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("攻击配置")]
    public LayerMask enemyLayer;          // 敌人图层（避免攻击到友方/场景）
    public Transform attackCheckPoint;    // 攻击检测点（比如武器碰撞点）
    public ParticleSystem attackVFX;      // 攻击特效
    public AudioClip attackSFX;           // 攻击音效

    private PlayerStats playerStats;
    private AudioSource audioSource;

    private void Awake()
    {
        // 获取组件引用
        playerStats = GetComponent<PlayerStats>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // 对外暴露的攻击触发方法（绑定到按键/动画事件）
    public void TriggerAttack()
    {
        // 1. 检查攻击冷却
        if (playerStats.currentAttackCooldown > 0)
        {
            Debug.Log("攻击冷却中，无法攻击！");
            return;
        }

        // 2. 检测攻击范围内的敌人
        Collider[] hitEnemies = Physics.OverlapSphere(
            attackCheckPoint.position,
            playerStats.attackRange,
            enemyLayer
        );

        if (hitEnemies.Length == 0)
        {
            Debug.Log("攻击范围内无敌人！");
            PlayAttackFeedback(); // 空挥也播放特效/音效
            ResetAttackCooldown();
            return;
        }

        // 3. 遍历命中的敌人，计算伤害并扣血
        foreach (var enemyCollider in hitEnemies)
        {
            EnemyStats enemyStats = enemyCollider.GetComponent<EnemyStats>();
            if (enemyStats == null) continue;

            // 4. 计算最终伤害（基础伤害 + 属性克制 + 暴击）
            float finalDamage = CalculateFinalDamage(enemyStats);

            // 5. 敌人扣血
            enemyStats.TakeDamage(finalDamage);
        }

        // 6. 播放攻击反馈（特效/音效）
        PlayAttackFeedback();

        // 7. 重置攻击冷却
        ResetAttackCooldown();
    }

    // 计算最终伤害（核心公式：攻击力 - 防御力 + 属性克制 + 暴击）
    private float CalculateFinalDamage(EnemyStats enemyStats)
    {
        // 基础伤害（攻击力 - 防御力，最低1点伤害）
        float baseDamage = Mathf.Max(playerStats.attackPower - enemyStats.defense, 1f);

        // 属性克制计算（火克木、水克火、木克水，克制时伤害×1.5）
        float elementMulti = 1f;
        if ((playerStats.attackElement == PlayerStats.ElementType.Fire && enemyStats.enemyElement == PlayerStats.ElementType.Wood) ||
            (playerStats.attackElement == PlayerStats.ElementType.Water && enemyStats.enemyElement == PlayerStats.ElementType.Fire) ||
            (playerStats.attackElement == PlayerStats.ElementType.Wood && enemyStats.enemyElement == PlayerStats.ElementType.Water))
        {
            elementMulti = 1.5f;
            Debug.Log("属性克制！伤害提升50%");
        }

        // 暴击判定
        float criticalMulti = 1f;
        if (Random.value <= playerStats.criticalRate)
        {
            criticalMulti = playerStats.criticalDamageMulti;
            Debug.Log("暴击！");
        }

        // 最终伤害 = 基础伤害 × 属性倍率 × 暴击倍率
        float finalDamage = baseDamage * elementMulti * criticalMulti;
        return finalDamage;
    }

    // 播放攻击特效/音效
    private void PlayAttackFeedback()
    {
        // 播放特效
        if (attackVFX != null)
        {
            attackVFX.Play();
        }

        // 播放音效
        if (attackSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSFX);
        }
    }

    // 重置攻击冷却
    private void ResetAttackCooldown()
    {
        playerStats.currentAttackCooldown = playerStats.attackCooldown;
    }

    // Gizmos绘制攻击范围（场景视图调试用）
    private void OnDrawGizmosSelected()
    {
        if (attackCheckPoint == null || playerStats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCheckPoint.position, playerStats.attackRange);
    }
}