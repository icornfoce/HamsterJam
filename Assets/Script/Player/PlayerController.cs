using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;

    [Header("Cinemachine Settings")]
    [Tooltip("ลาก Main Camera จากหน้าต่าง Hierarchy มาใส่ตรงนี้ (หรือปล่อยว่างไว้ระบบจะหาเอง)")]
    public Transform mainCamera; 

    [Header("Animation Settings")]
    [Tooltip("ลากตัวละครที่มี Animator มาใส่ช่องนี้")]
    public Animator animator;
    public string horizontalParam = "Horizontal"; // แกน X สำหรับ Blend Tree
    public string verticalParam = "Vertical";     // แกน Y สำหรับ Blend Tree
    public string speedParam = "Speed";           // ค่าความเร็วรวม
    public string hitTrigger = "Hit";             // ชื่อ Trigger ตอนโดนตี
    
    [Tooltip("ความเร็วในการเปลี่ยนท่าทาง (Animation Smoothing)")]
    public float animationDampTime = 0.1f;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ถ้าไม่ได้ลากใส่ ให้พยายามหา MainCamera อัตโนมัติ
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // บังคับให้ตัวละครหันหน้า (แกน Y) ไปทางเดียวกับที่กล้องมองเสมอ
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f);
        }

        HandleMovement();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Current Health: {currentHealth}");

        if (animator != null)
        {
            animator.SetTrigger(hitTrigger);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player is dead!");
        // เพิ่มเติม: เล่นท่าตาย หรือ Restart เกม
    }

    private void HandleMovement()
    {
        // รับค่า Input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // ป้องกันความเร็วเพิ่มขึ้นเมื่อเดินทะแยง
        Vector2 inputDir = Vector2.ClampMagnitude(new Vector2(x, z), 1f);
        x = inputDir.x;
        z = inputDir.y;

        // คำนวณทิศทางโดยอิงจากตัวละคร (ซึ่งหันหน้าตามกล้อง)
        Vector3 move = transform.right * x + transform.forward * z;

        // เคลื่อนที่ (ตัดแกน Y ออกเพราะไม่ต้องใช้แรงโน้มถ่วง/กระโดด)
        controller.Move(move * walkSpeed * Time.deltaTime);

        // --- อัปเดต Animation สำหรับ 2D Blend Tree ---
        if (animator != null)
        {
            animator.SetFloat(horizontalParam, x, animationDampTime, Time.deltaTime);
            animator.SetFloat(verticalParam, z, animationDampTime, Time.deltaTime);
            
            float inputMagnitude = inputDir.magnitude;
            animator.SetFloat(speedParam, inputMagnitude * walkSpeed, animationDampTime, Time.deltaTime);
        }
    }
}
