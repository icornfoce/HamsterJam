using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Firespirit : MonoBehaviour
{
    [Header("การเคลื่อนที่")]
    public float speed = 3f;
    
    [Header("Stats (พลังชีวิต)")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("การโจมตี (กระโดดระเบิด)")]
    public int attackDamage = 10;
    public float jumpDistance = 5f; // ระยะที่จะเริ่มกระโดดใส่ Player
    public float jumpHeight = 2f;   // ความสูงของการกระโดด
    public float jumpDuration = 0.5f; // ระยะเวลาที่ลอยในอากาศ (ความเร็วในการกระโดด)

    [Header("แอนิเมชัน")]
    public Animator animator;
    public string runAnimationBool = "isRunning"; 
    public string attackTrigger = "Attack"; // ช่องใส่ชื่อ Trigger Animation การโจมตี

    private Transform playerTransform;
    private NavMeshAgent agent;
    private bool isJumping = false;

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
        if (playerTransform != null && !isJumping)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // ถ้าระยะห่างน้อยกว่าหรือเท่ากับระยะกระโดด ให้เริ่มกระโดดโจมตี
            if (distanceToPlayer <= jumpDistance)
            {
                StartCoroutine(JumpAttack());
            }
            else
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
    }

    private IEnumerator JumpAttack()
    {
        isJumping = true;
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        agent.enabled = false; // ปิด NavMeshAgent ชั่วคราวเพื่อให้ควบคุมตำแหน่งเองได้

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // ปิดฟิสิกส์ชั่วคราวตอนกระโดด
        }

        // เล่นแอนิเมชันโจมตี
        if (animator != null)
        {
            animator.SetBool(runAnimationBool, false);
            animator.SetTrigger(attackTrigger);
        }

        Vector3 startPos = transform.position;
        // ล็อคเป้าหมายตำแหน่งผู้เล่นในตอนที่เริ่มกระโดด
        Vector3 targetPos = playerTransform.position; 
        float timeElapsed = 0f;

        while (timeElapsed < jumpDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / jumpDuration;

            // เคลื่อนที่แบบโค้ง (Parabola)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += jumpHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = currentPos;
            yield return null;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // เมื่อกระโดดเสร็จ เช็คระยะว่าโดน Player หรือไม่ (เผื่อกรณีไม่โดน Trigger/Collision ระหว่างทาง)
        if (Vector3.Distance(transform.position, playerTransform.position) <= 2f)
        {
            DoDamage(playerTransform.gameObject);
        }
        else
        {
            // ถ้ากระโดดพลาด ก็ให้ตายทันทีเหมือนกัน
            Die();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && isJumping)
        {
            DoDamage(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isJumping)
        {
            DoDamage(other.gameObject);
        }
    }

    private void DoDamage(GameObject targetPlayer)
    {
        PlayerHealth pHealth = targetPlayer.GetComponent<PlayerHealth>();
        if (pHealth != null)
        {
            pHealth.TakeDamage(attackDamage);
        }

        Debug.Log($"Firespirit พุ่งชน/ระเบิดใส่ Player! >>> เลือด Player ลดลงไป: {attackDamage}");

        // ทำดาเมจเสร็จแล้วตายทันที
        Die();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        Debug.Log($"<color=red>[Firespirit] โดนโจมตี {damage}! เลือดเหลือ {currentHealth}/{maxHealth}</color>");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[Firespirit] ตาย!");
        // TODO: ใส่ Animation หรือ Effect การตาย/ระเบิดตรงนี้
        Destroy(gameObject);
    }
}
