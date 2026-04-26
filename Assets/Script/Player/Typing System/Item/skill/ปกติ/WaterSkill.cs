using UnityEngine;

/// <summary>
/// Skill ของ Water: ยิงกระสุนน้ำพุ่งไปข้างหน้า ทำดาเมจศัตรูที่โดน
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill Water แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class WaterSkill : BaseItemSkill
{
    [Header("Projectile Settings")]
    [Tooltip("ความเร็วกระสุน")]
    public float speed = 15f;
    
    [Tooltip("ดาเมจที่ทำใส่ศัตรู")]
    public int damage = 20;
    
    [Tooltip("กระสุนจะหายไปหลังจากกี่วินาที")]
    public float lifetime = 5f;

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

        PlayVoice(playerTransform.position);

        isFired = true;
        Destroy(gameObject, lifetime);

        if (shootSFX != null)
            AudioSource.PlayClipAtPoint(shootSFX, transform.position);

        Debug.Log($"<color=blue>[WaterSkill] ยิงกระสุนน้ำ! ดาเมจ: {damage}</color>");
    }

    private void Update()
    {
        if (!isFired) return;

        float moveDistance = speed * Time.deltaTime;

        // Raycast ตรวจจับการชนล่วงหน้า
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.1f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // ส่งดาเมจไปให้ศัตรู
                hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

                Debug.Log($"<color=blue>[WaterSkill] โดนศัตรู! สร้างความเสียหาย {damage}</color>");
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
