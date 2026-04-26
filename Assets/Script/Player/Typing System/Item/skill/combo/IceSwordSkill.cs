using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Ice Sword: ปาดดาบน้ำแข็งไปด้านหน้า 1 ครั้ง
/// ทำดาเมจสูงให้ศัตรูทุกตัวที่อยู่ในมุมด้านหน้า + ชะลอความเร็ว
/// ผลลัพธ์จากการรวม Ice + Ice
/// </summary>
public class IceSwordSkill : BaseItemSkill
{
    [Header("Slash Settings")]
    [Tooltip("ระยะปาดดาบ")]
    public float slashRange = 6f;

    [Tooltip("มุมกว้างของการปาด (องศา)")]
    public float slashAngle = 120f;

    [Tooltip("ดาเมจ")]
    public int damage = 50;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ชะลอศัตรูที่โดน")]
    [Range(0f, 1f)]
    public float slowPercent = 0.5f;

    [Tooltip("ระยะเวลาที่ถูกชะลอ (วินาที)")]
    public float slowDuration = 3f;

    [Header("Visual / Audio")]
    [Tooltip("VFX วงปาดดาบ (ไม่บังคับ)")]
    public GameObject slashVFXPrefab;
    [Tooltip("เสียงปาดดาบ")]
    public AudioClip slashSFX;

    public override void Activate(Transform playerTransform)
    {
        PlayVoice(playerTransform.position);

        // เล่นเสียงปาด
        if (slashSFX != null)
            AudioSource.PlayClipAtPoint(slashSFX, playerTransform.position);

        // สร้าง VFX ปาดดาบ
        if (slashVFXPrefab != null)
        {
            GameObject vfx = Instantiate(slashVFXPrefab, 
                playerTransform.position + playerTransform.forward * 1f + Vector3.up * 1f, 
                playerTransform.rotation);
            Destroy(vfx, 2f);
        }

        // หาศัตรูทั้งหมดในระยะ
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, slashRange);
        int hitCount = 0;

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            // เช็คว่าศัตรูอยู่ในมุมด้านหน้าหรือไม่
            Vector3 dirToEnemy = (col.transform.position - playerTransform.position).normalized;
            float angle = Vector3.Angle(playerTransform.forward, dirToEnemy);

            if (angle > slashAngle * 0.5f) continue;

            // ทำดาเมจ
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hitCount++;

            // ชะลอศัตรู
            UnityEngine.AI.NavMeshAgent agent = col.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                SlowEffect existing = col.GetComponent<SlowEffect>();
                if (existing != null)
                    existing.RefreshSlow(slowPercent, slowDuration);
                else
                {
                    SlowEffect slow = col.gameObject.AddComponent<SlowEffect>();
                    slow.Setup(agent, slowPercent, slowDuration);
                }
            }

            Debug.Log($"<color=#AAEEFF>[IceSword] ปาดโดน {col.name}! ดาเมจ {damage}</color>");
        }

        Debug.Log($"<color=#AAEEFF>[IceSword] ปาดดาบน้ำแข็ง! โดนศัตรู {hitCount} ตัว</color>");

        Destroy(gameObject, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        // แสดงมุมปาดดาบใน Scene View
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, slashRange);
    }
}
