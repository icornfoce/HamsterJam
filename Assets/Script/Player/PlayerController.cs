using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -19.62f; // Stronger gravity feels better for games

    [Header("Cinemachine Settings")]
    [Tooltip("ลาก Main Camera จากหน้าต่าง Hierarchy มาใส่ตรงนี้ (หรือปล่อยว่างไว้ระบบจะหาเอง)")]
    public Transform mainCamera; 

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
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

    private void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep player grounded
        }

        // Use standard Legacy Input for movement (WASD or Arrow keys)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // Apply movement speed (Left Shift to sprint)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Jumping (Spacebar or default "Jump" input)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
