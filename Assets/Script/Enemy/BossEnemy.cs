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
public class BossEnemy : MonoBehaviour
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
    [Tooltip("เวลาหน่วงก่อนยิงกระสุนจริง (เพื่อให้ตรงกับแอนิเมชัน)")]
    public float      rangeAttackDelay = 0.5f;

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

    [Header("Cinemachine Camera")]
    [Tooltip("ลาก Cinemachine Virtual Camera ที่จะให้มองหน้า Furnace มาใส่ตรงนี้")]
    public GameObject introCamera;
    [Tooltip("ระยะเวลาที่กล้องจะสลับมาดูหน้า Furnace (วินาที)")]
    public float introDuration = 3f;

    [Header("UI / Health Bar")]
    [Tooltip("ลาก Script BossHealthBar (แบบแถบใหญ่บนจอ) มาใส่")]
    public BossHealthBar bossHealthBar;

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

    private bool isPerformingSkill = false;

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

        // ── Setup Health Bar ────────────────────────
        if (bossHealthBar != null)
        {
            bossHealthBar.UpdateHealth(maxHealth, maxHealth);
            bossHealthBar.Show();
        }

        // ── Trigger Intro Camera ────────────────────
        if (introCamera != null)
        {
            StartCoroutine(PlayIntroCamera());
        }
    }

    private System.Collections.IEnumerator PlayIntroCamera()
    {
        // เปิดกล้อง Intro (โดยการปรับ Priority ให้สูงกว่ากล้อง Player)
        // หมายเหตุ: ใน Inspector ต้องตั้ง Priority กล้อง Player ไว้ที่ 10 
        // และตั้งกล้อง Intro นี้ไว้ที่ 0 เป็นค่าเริ่มต้น (หรือตรงข้ามกันแล้วแต่ระบบ)
        // สมมติว่าเราเปิด/ปิด GameObject เพื่อสลับกล้องก็ได้เช่นกัน
        introCamera.SetActive(true);
        
        // รอเวลา
        yield return new WaitForSeconds(introDuration);

        // ปิดกล้อง Intro เพื่อกลับไปหากล้อง Player
        introCamera.SetActive(false);
    }

    // ══════════════════════════════════════════════
    private void Update()
    {
        if (playerTransform == null || !agent.isOnNavMesh) return;

        // ถ้ากำลังร่ายสกิล ให้หยุดเดินและไม่ทำอย่างอื่น
        if (isPerformingSkill)
        {
            agent.isStopped = true;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // ──── ประมวลผลพฤติกรรม ────
        if (dist <= meleeRange)
        {
            // ── โจมตีธรรมดา (Melee) ─────────────────────
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
            agent.isStopped = true;
            FaceTarget(playerTransform.position);

            StartCoroutine(RangeAttackRoutine());
            nextRangeTime = Time.time + rangeCooldown;
        }
        else
        {
            // ── วิ่งเข้าหา (Chase) ──────────────────────────────
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

        // ── Update Health Bar ───────────────────────
        if (bossHealthBar != null)
        {
            bossHealthBar.UpdateHealth(currentHealth, maxHealth);
        }

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

        // หยุดเดินขณะ Summon
        StartCoroutine(StopForSkill(1.5f)); // ปรับเวลาตามความยาวแอนิเมชัน Summon

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
    /// <summary>ยิงกระสุน (Range Attack) พร้อมดีเลย์</summary>
    private System.Collections.IEnumerator RangeAttackRoutine()
    {
        if (animator != null)
            animator.SetTrigger(rangeAttackTrig);

        // หยุดเดินขณะยิง
        isPerformingSkill = true;
        agent.isStopped = true;

        // ช่วงดีเลย์ง้างยิง: ให้หันหน้าตาม Player ตลอดเวลา
        float elapsed = 0f;
        while (elapsed < rangeAttackDelay)
        {
            if (playerTransform != null)
                FaceTarget(playerTransform.position);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ยิงกระสุน
        if (projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
            Vector3 dirToPlayer = (playerTransform.position + Vector3.up * 0.5f - spawnPos).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(dirToPlayer);

            GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

            RangeEnemyBullet bullet = proj.GetComponent<RangeEnemyBullet>();
            if (bullet != null)
            {
                bullet.damage = rangeDamage;
                bullet.speed = projectileSpeed;
                bullet.playerHealthRef = playerHealth;
            }
        }
        else
        {
            if (playerHealth != null)
                playerHealth.TakeDamage(rangeDamage);
        }

        Debug.Log($"[Furnace] Range Attack fired!");

        // หน่วงเวลาหลังยิงเล็กน้อยก่อนกลับไปเดิน (Recovery Time)
        yield return new WaitForSeconds(0.2f); 

        isPerformingSkill = false;
        if (agent.isActiveAndEnabled)
            agent.isStopped = false;
    }

    private System.Collections.IEnumerator StopForSkill(float duration)
    {
        isPerformingSkill = true;
        agent.isStopped = true;
        
        yield return new WaitForSeconds(duration);
        
        isPerformingSkill = false;
        if (agent.isActiveAndEnabled)
            agent.isStopped = false;
    }

    // ══════════════════════════════════════════════
    private void Die()
    {
        Debug.Log("[Furnace] ถูกทำลาย!");

        if (bossHealthBar != null)
            bossHealthBar.Hide();
        
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
