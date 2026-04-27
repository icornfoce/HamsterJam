using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class Rangeenemy : MonoBehaviour
{
    // ──────────────────────────────────────────
    [Header("การเคลื่อนที่")]
    public float moveSpeed       = 3f;

    [Header("Stats (พลังชีวิต)")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("ระยะโจมตี")]
    public float attackRange     = 10f;   // ระยะที่จะยิงได้
    public float preferredRange  = 7f;    // ระยะที่ต้องการรักษาไว้ (ห่างจาก Player)
    public float tooCloseRange   = 4f;    // ระยะที่ใกล้เกินไป → ถอยหนี

    [Header("กระสุน / โจมตี")]
    public GameObject projectilePrefab;   // Prefab กระสุน
    public Transform  firePoint;          // จุดยิงกระสุน (ถ้าไม่กำหนดจะใช้ตำแหน่ง Enemy)
    public float      projectileSpeed  = 12f;
    public int        attackDamage     = 10;
    public float      attackCooldown   = 2f;

    [Header("แอนิเมชัน")]
    public Animator   animator;
    public string     runAnimBool    = "isRunning";
    public string     attackTrigger  = "Attack";

    // ──────────────────────────────────────────
    private Transform    playerTransform;
    private NavMeshAgent agent;
    private float        nextAttackTime = 0f;
    private PlayerHealth playerHealth;          // cache ไว้เพื่อส่งให้กระสุนโดยตรง
    private bool         isAttacking    = false; // เพิ่มตัวแปรสถานะการโจมตี

    // สถานะของ AI
    private enum State { Chase, Hold, Retreat }
    private State currentState;

    // ──────────────────────────────────────────
    private void Start()
    {
        currentHealth = maxHealth;

        agent       = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        // หา Player จาก Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth    = player.GetComponent<PlayerHealth>(); // cache ไว้เลย
        } 
        // หา Animator อัตโนมัติถ้าไม่ได้กำหนด
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ──────────────────────────────────────────
    private void Update()
    {
        if (playerTransform == null || !agent.isOnNavMesh || isAttacking) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // ──── เลือก State ────
        if (distToPlayer <= tooCloseRange)
            currentState = State.Retreat;          // ใกล้เกินไป → ถอยหนี
        else if (distToPlayer <= preferredRange)
            currentState = State.Hold;             // อยู่ในระยะที่ดี → หยุดนิ่ง
        else
            currentState = State.Chase;            // ห่างเกินไป → วิ่งเข้าหา

        // ──── ประมวลผลตาม State ────
        switch (currentState)
        {
            case State.Chase:
                // วิ่งเข้าหา Player แต่หยุดก่อนถึง preferredRange
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                Vector3 targetPos   = playerTransform.position - dirToPlayer * preferredRange;
                agent.isStopped = false;
                agent.SetDestination(targetPos);
                break;

            case State.Hold:
                // หยุดอยู่กับที่ หันหน้าหา Player
                agent.isStopped = true;
                FaceTarget(playerTransform.position);
                break;

            case State.Retreat:
                // ถอยหนีออกห่างจาก Player
                Vector3 awayDir    = (transform.position - playerTransform.position).normalized;
                Vector3 retreatPos = transform.position + awayDir * preferredRange;
                agent.isStopped = false;
                agent.SetDestination(retreatPos);
                break;
        }

        // ──── แอนิเมชันวิ่ง ────
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            animator.SetBool(runAnimBool, isMoving);
        }

        // ──── โจมตีถ้าอยู่ในระยะ attackRange ────
        if (distToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackRoutine());
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // ──────────────────────────────────────────
    /// <summary>หันหน้าไปทาง Target (หมุนแค่แกน Y)</summary>
    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 10f
            );
    }

    // ──────────────────────────────────────────
    /// <summary>คอรูทีนจัดการการโจมตี (เพื่อให้หยุดเดินชั่วคราว)</summary>
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;

        // เล่น Animation
        if (animator != null)
        {
            animator.SetBool(runAnimBool, false); // ปิดแอนิเมชันวิ่ง
            animator.SetTrigger(attackTrigger);
        }

        // หันหน้าหา Player
        FaceTarget(playerTransform.position);

        if (projectilePrefab != null)
        {
            // ────── สร้างกระสุน ──────
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
            Vector3 dirToPlayer = (playerTransform.position + Vector3.up * 0.5f - spawnPos).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(dirToPlayer);

            GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

            RangeEnemyBullet bullet = proj.GetComponent<RangeEnemyBullet>();
            if (bullet != null)
            {
                bullet.damage          = attackDamage;
                bullet.speed           = projectileSpeed;
                bullet.playerHealthRef = playerHealth;
            }
        }
        else
        {
            // ────── Hitscan ──────
            PlayerHealth pHealth = playerTransform.GetComponent<PlayerHealth>();
            if (pHealth != null)
                pHealth.TakeDamage(attackDamage);
        }

        Debug.Log($"[RangeEnemy] โจมตี Player! ดาเมจ: {attackDamage}");

        // หยุดนิ่งสักพักตามระยะเวลาแอนิเมชัน (ปรับได้)
        yield return new WaitForSeconds(0.8f);

        isAttacking = false;
    }

    // ──────────────────────────────────────────
    /// <summary>แสดง Gizmos ให้เห็นในฉาก</summary>
    private void OnDrawGizmosSelected()
    {
        // ระยะ Attack (สีแดง)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // ระยะ Preferred (สีเหลือง)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredRange);

        // ระยะ Too Close (สีฟ้า)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, tooCloseRange);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        Debug.Log($"<color=red>[RangeEnemy] โดนโจมตี {damage}! เลือดเหลือ {currentHealth}/{maxHealth}</color>");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[RangeEnemy] ตาย!");
        // TODO: ใส่ Animation หรือ Effect การตายตรงนี้
        Destroy(gameObject);
    }
}
