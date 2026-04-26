using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Furnace Enemy
/// - ระยะใกล้มาก   → โจมตีธรรมดา (Melee)
/// - ระยะกลาง      → ยิงกระสุน (Range)
/// - ระยะไกล       → วิ่งเข้าหา (Chase)
/// - ทุก 20% HP ที่หายไป → Summon ลูกน้อง
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Furnace : MonoBehaviour
{
    // ══════════════════════════════════════════════
    [Header("HP")]
    public int maxHealth = 200;

    [Header("การเคลื่อนที่")]
    public float moveSpeed = 3f;

    // ══════════════════════════════════════════════
    [Header("ระยะโจมตี")]
    [Tooltip("ระยะโจมตีธรรมดา (Melee)")]
    public float meleeRange = 2.5f;

    [Tooltip("ระยะยิงกระสุน (Range) — ต้องมากกว่า meleeRange")]
    public float rangeAttackRange = 10f;

    // ══════════════════════════════════════════════
    [Header("โจมตีธรรมดา (Melee)")]
    public int   meleeDamage    = 20;
    public float meleeCooldown  = 1.2f;

    // ══════════════════════════════════════════════
    [Header("กระสุน / โจมตีไกล (Range)")]
    public GameObject projectilePrefab;
    public Transform  firePoint;
    public float      projectileSpeed  = 14f;
    public int        rangeDamage      = 12;
    public float      rangeCooldown    = 2f;

    // ══════════════════════════════════════════════
    [Header("Summon")]
    [Tooltip("Prefab ของลูกน้องที่จะ Summon")]
    public GameObject minionPrefab;

    [Tooltip("จำนวนลูกน้องที่ Summon ต่อครั้ง")]
    public int minionsPerSummon = 2;

    [Tooltip("รัศมีกระจายตัวของลูกน้องรอบๆ Furnace")]
    public float summonRadius = 3f;

    // ══════════════════════════════════════════════
    [Header("แอนิเมชัน")]
    public Animator animator;
    public string   runAnimBool      = "isRunning";
    public string   meleeAttackTrig  = "MeleeAttack";
    public string   rangeAttackTrig  = "RangeAttack";
    public string   summonTrig       = "Summon";
    public string   deathTrig        = "Die";
    
    [Tooltip("เวลาหน่วงก่อนตัวละครจะถูกทำลายหลังจากตาย (เพื่อให้เล่นแอนิเมชันตายจบ)")]
    public float    deathDelay       = 2f;

    // ══════════════════════════════════════════════
    //  Private
    // ══════════════════════════════════════════════
    private int          currentHealth;
    private int          summonThresholdIndex = 0;   // ดัชนี threshold ถัดไป

    // Threshold HP ที่ต้อง Summon: 80%, 60%, 40%, 20%
    private readonly float[] summonThresholds = { 0.8f, 0.6f, 0.4f, 0.2f };

    private Transform    playerTransform;
    private PlayerHealth playerHealth;
    private NavMeshAgent agent;

    private float nextMeleeTime = 0f;
    private float nextRangeTime = 0f;

    private enum State { Chase, RangeAttack, MeleeAttack }
    private State currentState;

    // ══════════════════════════════════════════════
    private void Start()
    {
        currentHealth = maxHealth;

        agent       = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth    = player.GetComponent<PlayerHealth>();
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ══════════════════════════════════════════════
    private void Update()
    {
        if (playerTransform == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // ──── ประมวลผลพฤติกรรม ────
        if (dist <= meleeRange)
        {
            // ── โจมตีธรรมดา (Melee) ─────────────────────
            currentState = State.MeleeAttack;
            agent.isStopped = true;
            FaceTarget(playerTransform.position);

            if (Time.time >= nextMeleeTime)
            {
                DoMeleeAttack();
                nextMeleeTime = Time.time + meleeCooldown;
            }
        }
        else if (dist <= rangeAttackRange && Time.time >= nextRangeTime)
        {
            // ── ยิงกระสุน (Range Attack) ─────────────
            currentState = State.RangeAttack;
            agent.isStopped = true;
            FaceTarget(playerTransform.position);

            DoRangeAttack();
            nextRangeTime = Time.time + rangeCooldown;
        }
        else
        {
            // ── วิ่งเข้าหา (Chase) ──────────────────────────────
            currentState = State.Chase;
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }

        // ──── แอนิเมชันวิ่ง ────
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            animator.SetBool(runAnimBool, isMoving);
        }
    }

    // ══════════════════════════════════════════════
    /// <summary>รับดาเมจจากผู้เล่นหรือสิ่งอื่น</summary>
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // ถ้าตายแล้ว ไม่รับดาเมจเพิ่ม

        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0);

        Debug.Log($"[Furnace] โดนดาเมจ {damage} | HP เหลือ {currentHealth}/{maxHealth}");

        // ── ตรวจ Summon Threshold ────────────────────
        CheckSummonThreshold();

        if (currentHealth <= 0)
            Die();
    }

    // ══════════════════════════════════════════════
    /// <summary>ตรวจสอบว่าถึง threshold ที่ต้อง Summon หรือยัง</summary>
    private void CheckSummonThreshold()
    {
        if (summonThresholdIndex >= summonThresholds.Length) return;

        float hpRatio = (float)currentHealth / maxHealth;

        // วนตรวจ threshold ทั้งหมดที่ยังไม่ผ่าน
        while (summonThresholdIndex < summonThresholds.Length &&
               hpRatio <= summonThresholds[summonThresholdIndex])
        {
            Debug.Log($"[Furnace] HP ถึง {summonThresholds[summonThresholdIndex] * 100}% → Summon ลูกน้อง!");
            SummonMinions();
            summonThresholdIndex++;
        }
    }

    // ══════════════════════════════════════════════
    /// <summary>Summon ลูกน้องรอบๆ Furnace</summary>
    private void SummonMinions()
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning("[Furnace] ไม่มี Minion Prefab กรุณากำหนดใน Inspector!");
            return;
        }

        if (animator != null)
            animator.SetTrigger(summonTrig);

        for (int i = 0; i < minionsPerSummon; i++)
        {
            // กระจายตัวเป็นวงกลมรอบๆ Furnace
            float angle    = (360f / minionsPerSummon) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * summonRadius;
            Vector3 spawnPos = transform.position + offset;

            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[Furnace] Summon ลูกน้อง {minionsPerSummon} ตัว!");
    }

    // ══════════════════════════════════════════════
    /// <summary>โจมตีธรรมดา (Melee)</summary>
    private void DoMeleeAttack()
    {
        if (animator != null)
            animator.SetTrigger(meleeAttackTrig);

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(meleeDamage);
            Debug.Log($"[Furnace] Melee โดน Player! ดาเมจ: {meleeDamage}");
        }
    }

    // ══════════════════════════════════════════════
    /// <summary>ยิงกระสุน (Range Attack)</summary>
    private void DoRangeAttack()
    {
        if (animator != null)
            animator.SetTrigger(rangeAttackTrig);

        if (projectilePrefab != null)
        {
            Vector3 spawnPos    = firePoint != null ? firePoint.position : transform.position + Vector3.up;
            Vector3 dirToPlayer = (playerTransform.position + Vector3.up * 0.5f - spawnPos).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(dirToPlayer);

            GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

            // ใช้ RangeEnemyBullet เดิม
            RangeEnemyBullet bullet = proj.GetComponent<RangeEnemyBullet>();
            if (bullet != null)
            {
                bullet.damage          = rangeDamage;
                bullet.speed           = projectileSpeed;
                bullet.playerHealthRef = playerHealth;
            }
        }
        else
        {
            // Hitscan fallback ถ้าไม่มี Prefab
            Debug.Log("[Furnace] ไม่มี Projectile Prefab → ใช้ Hitscan");
            if (playerHealth != null)
                playerHealth.TakeDamage(rangeDamage);
        }

        Debug.Log($"[Furnace] Range Attack! ดาเมจ: {rangeDamage}");
    }

    // ══════════════════════════════════════════════
    private void Die()
    {
        Debug.Log("[Furnace] ถูกทำลาย!");
        
        // เล่น Death Animation
        if (animator != null)
            animator.SetTrigger(deathTrig);
            
        // ปิด Agent ไม่ให้เดินต่อ
        if (agent != null)
            agent.enabled = false;
            
        // ปิด Collider ป้องกันการโดนโจมตีซ้ำหรือเดินชน
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
            
        // ปิดสคริปต์นี้เพื่อหยุดการ Update (จะได้ไม่โจมตีต่อตอนกำลังตาย)
        this.enabled = false;

        // รอหน่วงเวลา (deathDelay) ก่อนทำลาย object เพื่อให้แอนิเมชันตายเล่นจบ
        Destroy(gameObject, deathDelay);
    }

    // ══════════════════════════════════════════════
    /// <summary>หันหน้าไปทาง Target (แกน Y เท่านั้น)</summary>
    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 10f
            );
    }

    // ══════════════════════════════════════════════
    /// <summary>Gizmos แสดงระยะในฉาก</summary>
    private void OnDrawGizmosSelected()
    {
        // Melee Range (สีแดง)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Range Attack Range (สีส้ม)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangeAttackRange);

        // Summon Radius (สีม่วง)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}
