using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("การเคลื่อนที่")]
    public float speed = 3f;
    
    [Header("Stats (พลังชีวิต)")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("การโจมตี")]
    public float attackRange = 2f; 
    public int attackDamage = 10;
    public float attackCooldown = 1f;

    [Header("แอนิเมชัน")]
    public Animator animator;
    public string runAnimationBool = "isRunning"; 
    public string attackTrigger = "Attack";       

    private Transform playerTransform;
    private NavMeshAgent agent;
    private float nextAttackTime = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;

        // ให้ศัตรูหา Player เจอเองเมื่อเริ่มเกมจาก Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= attackRange)
            {
                // หยุดเดินและโจมตี
                agent.isStopped = true;
                
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                // เดินตามเป้าหมาย (Player)
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTransform.position);
                }
            }

            // จัดการแอนิเมชัน
            if (animator != null)
            {
                bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
                animator.SetBool(runAnimationBool, isMoving);
            }
        }
    }

    private void Attack()
    {
        // เล่นแอนิเมชันโจมตี
        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        // ทำดาเมจ
        PlayerHealth pHealth = playerTransform.GetComponent<PlayerHealth>();
        if (pHealth != null)
        {
            pHealth.TakeDamage(attackDamage);
        }

        Debug.Log($"Enemy โจมตี Player! >>> ดาเมจ: {attackDamage}");
    }

    // เก็บการชนไว้เป็น Fallback หรือสำหรับกรณีวิ่งชน
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        Debug.Log($"<color=red>[Enemy] โดนโจมตี {damage}! เลือดเหลือ {currentHealth}/{maxHealth}</color>");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[Enemy] ตาย!");
        // TODO: ใส่ Animation หรือ Effect การตายตรงนี้
        Destroy(gameObject);
    }
}
