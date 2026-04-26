using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Snow Ball Shot Gun: ยิงก้อนหิมะกระจายออกไปหลายลูกเหมือนลูกซอง
/// ผลลัพธ์จากการรวม Ice + Water
/// </summary>
public class SnowBallShotgunSkill : BaseItemSkill
{
    [Header("Shotgun Settings")]
    [Tooltip("จำนวนลูกหิมะที่ยิงออกไปต่อครั้ง")]
    public int pelletCount = 7;

    [Tooltip("มุมกระจาย (องศา)")]
    public float spreadAngle = 35f;

    [Tooltip("ความเร็วลูกหิมะ")]
    public float speed = 20f;

    [Tooltip("ดาเมจต่อลูก")]
    public int damagePerPellet = 12;

    [Tooltip("ลูกหิมะจะหายไปหลังจากกี่วินาที")]
    public float lifetime = 3f;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ชะลอศัตรูที่โดน")]
    [Range(0f, 1f)]
    public float slowPercent = 0.3f;

    [Tooltip("ระยะเวลาที่ถูกชะลอ (วินาที)")]
    public float slowDuration = 2f;

    [Header("Visual / Audio")]
    [Tooltip("Prefab ลูกหิมะ (ถ้าไม่มีจะใช้ Raycast แทน)")]
    public GameObject snowballPrefab;
    public GameObject hitVFXPrefab;
    public AudioClip shotgunSFX;
    public AudioClip hitSFX;

    public override void Activate(Transform playerTransform)
    {
        Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 1f;

        PlayVoice(playerTransform.position);

        if (shotgunSFX != null)
            AudioSource.PlayClipAtPoint(shotgunSFX, spawnPos);

        // ยิงลูกหิมะกระจายออกไป
        for (int i = 0; i < pelletCount; i++)
        {
            // คำนวณมุมกระจาย (กระจายเท่าๆ กันในแนวนอน + สุ่มแนวตั้งเล็กน้อย)
            float t = pelletCount > 1 ? (float)i / (pelletCount - 1) : 0.5f;
            float horizontalAngle = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
            float verticalAngle = Random.Range(-spreadAngle * 0.15f, spreadAngle * 0.15f);

            Quaternion rotation = playerTransform.rotation
                * Quaternion.Euler(verticalAngle, horizontalAngle, 0f);

            if (snowballPrefab != null)
            {
                // สร้างลูกหิมะจริง
                GameObject pellet = Instantiate(snowballPrefab, spawnPos, rotation);
                SnowballPellet script = pellet.GetComponent<SnowballPellet>();
                if (script == null) script = pellet.AddComponent<SnowballPellet>();

                script.Setup(speed, damagePerPellet, lifetime, slowPercent, slowDuration, hitVFXPrefab, hitSFX);
            }
            else
            {
                // ถ้าไม่มี Prefab ให้ใช้ Raycast ยิงทันที
                Vector3 dir = rotation * Vector3.forward;
                if (Physics.Raycast(spawnPos, dir, out RaycastHit hit, 50f))
                {
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        hit.collider.SendMessage("TakeDamage", damagePerPellet, SendMessageOptions.DontRequireReceiver);
                        ApplySlow(hit.collider.gameObject);
                    }
                }
            }
        }

        Debug.Log($"<color=#DDDDFF>[SnowBallShotgun] ยิงก้อนหิมะ {pelletCount} ลูก! ดาเมจรวมสูงสุด: {pelletCount * damagePerPellet}</color>");

        // ถ้าไม่มี Prefab ก็ลบตัวเองทิ้งเลย
        if (snowballPrefab == null)
            Destroy(gameObject, 0.5f);
        else
            Destroy(gameObject, lifetime + 1f);
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
                SlowEffect slow = enemy.AddComponent<SlowEffect>();
                slow.Setup(agent, slowPercent, slowDuration);
            }
        }
    }
}
