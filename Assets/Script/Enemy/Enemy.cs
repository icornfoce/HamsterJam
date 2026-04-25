using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("การเคลื่อนที่")]
    public float speed = 3f;
    
    [Header("การโจมตี (การสัมผัส)")]
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
            // เดินตามเป้าหมาย (Player)
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(playerTransform.position);
            }

            // เปิดแอนิเมชันวิ่ง
            if (animator != null)
            {
                bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
                animator.SetBool(runAnimationBool, isMoving);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DoDamage(collision.gameObject);
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            DoDamage(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DoDamage(other.gameObject);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DoDamage(other.gameObject);
        }
    }

    private void DoDamage(GameObject targetPlayer)
    {
        if (Time.time >= nextAttackTime)
        {
            // เล่นแอนิเมชันโจมตี
            if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
            }

            // --- ชั่วคราว: ปิดระบบลดเลือดไปก่อนตามที่ต้องการ ---
            PlayerHealth pHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(attackDamage);
            }

            // ให้พิมพ์แค่ Debug ลง Console ตอนนี้
            Debug.Log($"Enemy แตะโดนตัว Player! >>> เลือด Player ลดลงไป: {attackDamage}");

            nextAttackTime = Time.time + attackCooldown;
        }
    }
}
