using UnityEngine;

/// <summary>
/// กระสุน Range Enemy
/// เคลื่อนที่ด้วย Transform.Translate และใช้ Raycast ตรวจการชน
/// ไม่ต้องพึ่ง Rigidbody หรือ Is Trigger
/// </summary>
public class RangeEnemyBullet : MonoBehaviour
{
    [HideInInspector] public int          damage          = 10;
    [HideInInspector] public float        speed           = 12f;
    [HideInInspector] public PlayerHealth playerHealthRef = null;

    [Tooltip("กระสุนจะหายไปหลังจากกี่วินาทีถ้ายังไม่โดน")]
    public float lifetime = 5f;

    // ──────────────────────────────────────────
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float moveDistance = speed * Time.deltaTime;

        // ── Raycast ไปข้างหน้าระยะที่กระสุนจะเดินในเฟรมนี้ ──
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.1f))
        {
            Debug.Log($"[Bullet] Raycast โดน: {hit.collider.gameObject.name} | Tag: {hit.collider.tag}");

            if (hit.collider.CompareTag("Player"))
            {
                // ใช้ reference ที่ Range Enemy ส่งมา (แน่ใจว่า instance ถูก)
                PlayerHealth pHealth = playerHealthRef != null
                    ? playerHealthRef
                    : hit.collider.GetComponentInParent<PlayerHealth>();

                if (pHealth != null)
                {
                    pHealth.TakeDamage(damage);
                    Debug.Log($"[Bullet] โดน Player! ดาเมจ: {damage}");
                }
                else
                {
                    Debug.LogWarning("[Bullet] ชน Player แต่หา PlayerHealth ไม่เจอ!");
                }

                Destroy(gameObject);
                return;
            }
            else if (!hit.collider.CompareTag("Enemy"))
            {
                // ชนอะไรก็ตามที่ไม่ใช่ Enemy → หายไป
                Destroy(gameObject);
                return;
            }
        }

        // เคลื่อนที่ไปข้างหน้า
        transform.Translate(Vector3.forward * moveDistance);
    }
}
