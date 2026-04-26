using UnityEngine;

/// <summary>
/// Skill ของ Dry Ice: ยิงก้อนน้ำแข็งพุ่งไปข้างหน้า ทำดาเมจและชะลอศัตรูที่โดน
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill Dry Ice แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class DryIceSkill : BaseItemSkill
{
    [Header("Projectile Settings")]
    [Tooltip("ความเร็วก้อนน้ำแข็ง")]
    public float speed = 12f;
    
    [Tooltip("ดาเมจที่ทำใส่ศัตรู")]
    public int damage = 35;
    
    [Tooltip("ก้อนน้ำแข็งจะหายไปหลังจากกี่วินาที")]
    public float lifetime = 5f;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ที่จะชะลอศัตรู (0.5 = ช้าลง 50%)")]
    [Range(0f, 1f)]
    public float slowPercent = 0.5f;

    [Tooltip("ระยะเวลาที่ศัตรูจะถูกชะลอ (วินาที)")]
    public float slowDuration = 3f;

    [Header("Visual / Audio")]
    [Tooltip("VFX ที่จะเกิดเมื่อโดนเป้าหมาย (ไม่บังคับ)")]
    public GameObject hitVFXPrefab;
    [Tooltip("เสียงตอนยิง (ไม่บังคับ)")]
    public AudioClip shootSFX;
    [Tooltip("เสียงตอนโดนเป้าหมาย (ไม่บังคับ)")]
    public AudioClip hitSFX;

    private bool isFired = false;

    public override void Activate(Transform playerTransform)
    {
        // จัดตำแหน่งให้อยู่ข้างหน้าผู้เล่นเล็กน้อย
        transform.position = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 1f;
        transform.rotation = playerTransform.rotation;

        isFired = true;
        Destroy(gameObject, lifetime);

        if (shootSFX != null)
            AudioSource.PlayClipAtPoint(shootSFX, transform.position);

        Debug.Log($"<color=#88DDFF>[DryIceSkill] ยิงก้อนน้ำแข็ง! ดาเมจ: {damage}, ชะลอ: {slowPercent * 100}% เป็นเวลา {slowDuration} วินาที</color>");
    }

    private void Update()
    {
        if (!isFired) return;

        float moveDistance = speed * Time.deltaTime;

        // Raycast ตรวจจับการชนล่วงหน้า
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.2f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // ส่งดาเมจไปให้ศัตรู
                hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

                // ชะลอศัตรู (ถ้ามี NavMeshAgent)
                ApplySlowEffect(hit.collider.gameObject);

                Debug.Log($"<color=#88DDFF>[DryIceSkill] โดนศัตรู {hit.collider.name}! สร้างความเสียหาย {damage} + ชะลอ</color>");
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
            else if (!hit.collider.CompareTag("Player"))
            {
                // ชนสิ่งอื่นที่ไม่ใช่ Player → หายไป
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
        }

        // เคลื่อนที่ไปข้างหน้า
        transform.Translate(Vector3.forward * moveDistance, Space.Self);
    }

    private void ApplySlowEffect(GameObject enemy)
    {
        // หา NavMeshAgent เพื่อลดความเร็ว
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            // ใช้ SlowEffect Component เพื่อจัดการคืนค่าความเร็วอัตโนมัติ
            SlowEffect existingSlow = enemy.GetComponent<SlowEffect>();
            if (existingSlow != null)
            {
                // ถ้ามี SlowEffect อยู่แล้ว ให้รีเซ็ตเวลานับถอยหลัง
                existingSlow.RefreshSlow(slowPercent, slowDuration);
            }
            else
            {
                // สร้าง SlowEffect ใหม่
                SlowEffect slow = enemy.AddComponent<SlowEffect>();
                slow.Setup(agent, slowPercent, slowDuration);
            }
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(hitVFXPrefab, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, position);
        }
    }
}
