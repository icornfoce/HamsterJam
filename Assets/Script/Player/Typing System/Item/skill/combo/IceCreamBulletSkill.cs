using UnityEngine;

/// <summary>
/// Skill ของ Ice Cream Bullets: ยิงกระสุนไอศกรีมพุ่งไปข้างหน้า 1 ครั้ง ทำดาเมจศัตรูที่โดน
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class IceCreamBulletSkill : BaseItemSkill
{
    [Header("Projectile Settings")]
    [Tooltip("ความเร็วกระสุน")]
    public float speed = 18f;
    
    [Tooltip("ดาเมจที่ทำใส่ศัตรู")]
    public int damage = 25;
    
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
        transform.position = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 1f;
        transform.rotation = playerTransform.rotation;

        PlayVoice(playerTransform.position);

        isFired = true;
        Destroy(gameObject, lifetime);

        if (shootSFX != null)
            AudioSource.PlayClipAtPoint(shootSFX, transform.position);

        Debug.Log($"<color=#FF99CC>[IceCreamBullet] ยิงกระสุนไอศกรีม! ดาเมจ: {damage}</color>");
    }

    private void Update()
    {
        if (!isFired) return;

        float moveDistance = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.2f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                Debug.Log($"<color=#FF99CC>[IceCreamBullet] โดนศัตรู {hit.collider.name}! ดาเมจ {damage}</color>");
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
            else if (!hit.collider.CompareTag("Player"))
            {
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
        }

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
