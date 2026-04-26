using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Ice Sword: ปาดาบน้ำแข็ง (ขว้างดาบ) พุ่งไปด้านหน้า
/// ทำดาเมจสูงและทะลวงฟันศัตรูในแนวเส้นตรง พร้อมชะลอความเร็ว
/// ผลลัพธ์จากการรวม Ice + Ice
/// </summary>
public class IceSwordSkill : BaseItemSkill
{
    [Header("Throw Settings")]
    [Tooltip("ความเร็วของดาบที่ปาออกไป")]
    public float throwSpeed = 25f;

    [Tooltip("ดาเมจ")]
    public int damage = 50;
    
    [Tooltip("ดาบจะหายไปหลังจากกี่วินาที")]
    public float lifetime = 3f;

    [Tooltip("ให้ดาบทะลุศัตรูได้หรือไม่ (ถ้าไม่ทะลุ จะหายไปเมื่อโดนตัวแรก)")]
    public bool pierceEnemies = true;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ชะลอศัตรูที่โดน")]
    [Range(0f, 1f)]
    public float slowPercent = 0.5f;

    [Tooltip("ระยะเวลาที่ถูกชะลอ (วินาที)")]
    public float slowDuration = 3f;

    [Header("Visual / Audio")]
    [Tooltip("VFX เมื่อโดนศัตรู (ไม่บังคับ)")]
    public GameObject hitVFXPrefab;
    [Tooltip("เสียงตอนปาดาบ")]
    public AudioClip throwSFX;
    [Tooltip("เสียงตอนดาบโดนศัตรู")]
    public AudioClip hitSFX;

    private bool isThrown = false;
    private List<Collider> hitEnemies = new List<Collider>(); // เก็บรายชื่อศัตรูที่โดนไปแล้ว (กรณีทะลุ)

    public override void Activate(Transform playerTransform)
    {
        // จัดตำแหน่งให้อยู่ด้านหน้าผู้เล่นเล็กน้อย และขยับขึ้นมาประมาณระดับอก
        transform.position = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 1f;
        transform.rotation = playerTransform.rotation;

        PlayVoice(playerTransform.position);

        // เล่นเสียงปาดาบ
        if (throwSFX != null)
            AudioSource.PlayClipAtPoint(throwSFX, transform.position);

        isThrown = true;
        Destroy(gameObject, lifetime);

        Debug.Log($"<color=#AAEEFF>[IceSword] ปาดาบน้ำแข็ง! ดาเมจ: {damage}</color>");
    }

    private void Update()
    {
        if (!isThrown) return;

        float moveDistance = throwSpeed * Time.deltaTime;

        // ใช้ SphereCast เพื่อให้มีขนาดความกว้างของดาบในการชน ไม่ใช่แค่เส้นบางๆ
        if (Physics.SphereCast(transform.position, 0.5f, transform.forward, out RaycastHit hit, moveDistance + 0.1f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // ถ้ายังไม่เคยโดนตัวนี้
                if (!hitEnemies.Contains(hit.collider))
                {
                    hitEnemies.Add(hit.collider);

                    // ทำดาเมจ
                    hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

                    // ชะลอศัตรู
                    ApplySlow(hit.collider.gameObject);
                    SpawnHitEffect(hit.point);

                    Debug.Log($"<color=#AAEEFF>[IceSword] ดาบพุ่งแทง {hit.collider.name}! ดาเมจ {damage}</color>");

                    if (!pierceEnemies)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            else if (!hit.collider.CompareTag("Player"))
            {
                // ชนกำแพงหรือสิ่งกีดขวางอื่นๆ → หายไป
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
        }

        // เคลื่อนที่ไปข้างหน้า
        transform.Translate(Vector3.forward * moveDistance, Space.Self);
    }

    private void ApplySlow(GameObject enemy)
    {
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            SlowEffect existing = enemy.GetComponent<SlowEffect>();
            if (existing != null)
                existing.RefreshSlow(slowPercent, slowDuration);
            else
            {
                SlowEffect slow = enemy.gameObject.AddComponent<SlowEffect>();
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
