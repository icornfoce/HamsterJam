using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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

    [Tooltip("คูลดาวน์ระหว่างการ Summon แต่ละครั้ง (CC)")]
    public float summonCooldown = 4f;

    [Tooltip("เวลาหน่วงก่อนลูกน้องจะโผล่ออกมาจริง (เพื่อให้ตรงกับแอนิเมชันร่าย)")]
    public float summonSpawnDelay = 1.0f;

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

    [Header("เสียง (Sound Effects)")]
    public AudioClip spawnSFX;
    public AudioClip walkSFX;
    public AudioClip meleeSFX;
    public AudioClip rangeSFX;
    public AudioClip summonSFX;
    public AudioClip deathSFX;
    private AudioSource audioSource;
    private AudioSource walkAudioSource;

    // ══════════════════════════════════════════════
    //  Private
    // ══════════════════════════════════════════════
    private int          currentHealth;
    private int          summonThresholdIndex = 0;   // ดัชนี threshold ถัดไป
    private int          pendingSummonCount   = 0;   // จำนวนโควตาที่รอ Summon
    private float        nextSummonTime       = 0f;  // เวลาที่จะ Summon ครั้งถัดไปได้

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // ตั้งค่า AudioSource สำหรับเสียงเดิน (Loop)
        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.clip = walkSFX;
        walkAudioSource.loop = true;
        walkAudioSource.playOnAwake = false;
        walkAudioSource.spatialBlend = 1f; // ให้เป็นเสียง 3D

        // ── Trigger Intro / Setup UI ────────────────────
        StartCoroutine(HandleBossIntro());
    }

    private IEnumerator HandleBossIntro()
    {
        // เล่นเสียงเกิดทันที
        PlaySound(spawnSFX);

        // รอเล็กน้อยเพื่อให้แอนิเมชันเริ่ม หรือให้ผู้เล่นเตรียมตัว
        yield return new WaitForSeconds(0.2f);

        // ── Setup Health Bar ────────────────────────
        if (bossHealthBar == null)
        {
            // พยายามหาแม้จะถูกปิด (Inactive) อยู่ในฉาก
            BossHealthBar[] allBars = Resources.FindObjectsOfTypeAll<BossHealthBar>();
            foreach (var bar in allBars)
            {
                // ตรวจสอบว่าเป็น Object ในฉากจริงๆ (ไม่ใช่ต้นฉบับใน Project/Asset)
                if (bar.gameObject.scene.name != null)
                {
                    bossHealthBar = bar;
                    break;
                }
            }
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(true); // เปิดใช้งาน Object เผื่อถูกปิดไว้
            bossHealthBar.UpdateHealth(currentHealth, maxHealth);
            bossHealthBar.Show();
        }

        // ── Trigger Intro Camera ────────────────────
        if (introCamera != null)
        {
            introCamera.SetActive(true);
            yield return new WaitForSeconds(introDuration);
            introCamera.SetActive(false);
        }
    }


    // ══════════════════════════════════════════════
    private void Update()
    {
        if (playerTransform == null) return;

        // ถ้ากำลังร่ายสกิล ให้หยุดเดินและไม่ทำอย่างอื่น
        if (isPerformingSkill)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // ──── ประมวลผลพฤติกรรมตามลำดับความสำคัญ ────

        // 1. Summon (สำคัญที่สุด - ถ้าถึง Threshold ให้ Summon ก่อนแม้จะอยู่ใกล้ Player)
        if (pendingSummonCount > 0 && Time.time >= nextSummonTime)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
                
            isPerformingSkill = true; // ล็อคสถานะทันที
            Debug.Log($"<color=cyan>[Boss] กำลังเริ่มขั้นตอน Summon... (คิวคงเหลือ: {pendingSummonCount})</color>");
            
            StartCoroutine(SummonRoutine()); // ใช้ Coroutine เพื่อให้มี Delay
            
            pendingSummonCount--;
            nextSummonTime = Time.time + summonCooldown;
        }
        // 2. Melee Attack (ถ้าไม่อยู่ในขั้นตอน Summon และเข้าระยะ)
        else if (dist <= meleeRange)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
                
            FaceTarget(playerTransform.position);

            if (Time.time >= nextMeleeTime)
            {
                DoMeleeAttack();
                nextMeleeTime = Time.time + meleeCooldown;
            }
        }
        // 3. Range Attack (ถ้า Summon ไม่ได้และอยู่ในระยะยิง)
        else if (dist <= rangeAttackRange && Time.time >= nextRangeTime)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
                
            FaceTarget(playerTransform.position);

            StartCoroutine(RangeAttackRoutine());
            nextRangeTime = Time.time + rangeCooldown;
        }
        // 4. Chase (กรณีอื่นทั้งหมด)
        else
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
            }
        }

        // ──── แอนิเมชันวิ่ง และ เสียงเดิน ────
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            animator.SetBool(runAnimBool, isMoving);

            // จัดการเสียงเดิน
            if (isMoving && !walkAudioSource.isPlaying)
            {
                if (walkSFX != null) walkAudioSource.Play();
            }
            else if (!isMoving && walkAudioSource.isPlaying)
            {
                walkAudioSource.Stop();
            }
        }
    }

    // ══════════════════════════════════════════════
    /// <summary>รับดาเมจจากผู้เล่นหรือสิ่งอื่น</summary>
    public void TakeDamage(int damage)
    {
        // เช็คว่ามีดาเมจเข้ามาจริงไหม
        Debug.Log($"<color=yellow>[Boss] โดนโจมตี! ดาเมจที่ได้รับ: {damage} | เลือดคงเหลือ: {currentHealth - damage}/{maxHealth}</color>");

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
            Debug.Log($"[Furnace] HP ถึง {summonThresholds[summonThresholdIndex] * 100}% → จองคิว Summon!");
            pendingSummonCount++; // เพิ่มจำนวนโควตาที่ต้องเรียกใช้
            summonThresholdIndex++;
        }
    }

    private IEnumerator SummonRoutine()
    {
        if (animator != null)
            animator.SetTrigger(summonTrig);

        PlaySound(summonSFX);

        // หยุดเดินขณะ Summon (ใช้เวลาทั้งหมดของท่า)
        StartCoroutine(StopForSkill(1.5f + summonSpawnDelay)); 

        // รอเวลาให้แอนิเมชันเล่นไปถึงจังหวะที่ควรจะเกิดลูกน้อง
        yield return new WaitForSeconds(summonSpawnDelay);

        SummonMinions();
    }

    // ══════════════════════════════════════════════
    /// <summary>Summon ลูกน้องรอบๆ Boss</summary>
    private void SummonMinions()
    {
        Debug.Log($"[Boss] กำลังทำงานใน SummonMinions()... (จำนวนที่ต้องการ: {minionsPerSummon})");

        if (minionPrefab == null)
        {
            Debug.LogError("[Boss] ไม่สามารถ Summon ได้เนื่องจากไม่มี Minion Prefab! กรุณาลากใส่ใน Inspector ของ BossEnemy");
            isPerformingSkill = false;
            return;
        }

        if (minionsPerSummon <= 0)
        {
            Debug.LogWarning("[Boss] minionsPerSummon มีค่าเป็น 0 หรือติดลบ ระบบจะปรับเป็น 1 ตัวให้อัตโนมัติ");
            minionsPerSummon = 1;
        }

        int actualSpawned = 0;
        for (int i = 0; i < minionsPerSummon; i++)
        {
            // กระจายตัวเป็นวงกลมรอบๆ Boss
            float angle    = (360f / minionsPerSummon) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * summonRadius;
            Vector3 targetPos = transform.position + offset;

            // ตรวจสอบตำแหน่งบน NavMesh เพื่อไม่ให้ลูกน้องเกิดนอกแมพหรือในกำแพง
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 4f, NavMesh.AllAreas))
            {
                GameObject minion = Instantiate(minionPrefab, hit.position, Quaternion.identity);
                Debug.Log($"[Boss] สร้างลูกน้องตัวที่ {actualSpawned + 1} สำเร็จที่ตำแหน่ง NavMesh: {hit.position}");
                actualSpawned++;
            }
            else
            {
                // ถ้าหาจุดบน NavMesh ไม่ได้ ให้เกิดที่ตำแหน่ง Boss แทน (Safe fallback)
                GameObject minion = Instantiate(minionPrefab, transform.position, Quaternion.identity);
                Debug.Log($"[Boss] หาจุดบน NavMesh ไม่ได้ จึงสร้างลูกน้องตัวที่ {actualSpawned + 1} ที่ตำแหน่งบอสแทน: {transform.position}");
                actualSpawned++;
            }
        }

        Debug.Log($"<color=green>[Boss] ขั้นตอนการ Summon ทั้งหมดเสร็จสิ้น! รวมสร้างได้: {actualSpawned} ตัว</color>");
    }

    // ══════════════════════════════════════════════
    /// <summary>โจมตีธรรมดา (Melee)</summary>
    private void DoMeleeAttack()
    {
        if (animator != null)
            animator.SetTrigger(meleeAttackTrig);

        PlaySound(meleeSFX);

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(meleeDamage);
            Debug.Log($"[Furnace] Melee โดน Player! ดาเมจ: {meleeDamage}");
        }
    }

    // ══════════════════════════════════════════════
    /// <summary>ยิงกระสุน (Range Attack) พร้อมดีเลย์</summary>
    private IEnumerator RangeAttackRoutine()
    {
        if (animator != null)
            animator.SetTrigger(rangeAttackTrig);

        PlaySound(rangeSFX);

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

    private IEnumerator StopForSkill(float duration)
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
        Debug.Log("[Boss] ถูกทำลาย!");

        if (walkAudioSource != null) walkAudioSource.Stop();
        PlaySound(deathSFX);

        if (bossHealthBar != null)
            bossHealthBar.Hide();
        
        // เล่น Death Animation
        if (animator != null)
            animator.SetTrigger(deathTrig);

        // Play Boss Death Cutscene
        if (VideoManager.Instance != null)
        {
            VideoManager.Instance.PlayBossDeath(() => {
                Debug.Log("Boss death cutscene finished.");
            });
        }
            
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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
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
