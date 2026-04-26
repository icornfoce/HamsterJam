using UnityEngine;

/// <summary>
/// Skill ของ Big Ice Cream: ฮีล HP ให้ผู้เล่นเยอะกว่า Ice Cream ปกติ
/// ผลลัพธ์จากการรวม Ice Cream + Ice Cream
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill Big Ice Cream แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class BigIceCreamSkill : BaseItemSkill
{
    [Header("Heal Settings")]
    [Tooltip("จำนวน HP ที่จะฮีลให้ผู้เล่น (มากกว่า Ice Cream ธรรมดา)")]
    public float healAmount = 70f;

    [Header("Visual / Audio")]
    [Tooltip("VFX ที่จะเกิดตรงตัวผู้เล่นตอนฮีล (ไม่บังคับ)")]
    public GameObject healVFXPrefab;
    [Tooltip("เสียงที่จะเล่นตอนฮีล (ไม่บังคับ)")]
    public AudioClip healSFX;

    public override void Activate(Transform playerTransform)
    {
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[BigIceCreamSkill] ไม่พบ PlayerHealth! Skill ไม่ทำงาน");
            Destroy(gameObject);
            return;
        }

        playerHealth.Heal(healAmount);
        PlayVoice(playerTransform.position);
        Debug.Log($"<color=#FF88CC>[BigIceCreamSkill] ฮีลผู้เล่น +{healAmount} HP! (Big Size!)</color>");

        // เล่นเสียงฮีล
        if (healSFX != null)
        {
            AudioSource.PlayClipAtPoint(healSFX, playerTransform.position);
        }

        // เกิด VFX ตรงตัวผู้เล่น
        if (healVFXPrefab != null)
        {
            GameObject vfx = Instantiate(healVFXPrefab, playerTransform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // ทำลายตัวเองหลังจากทำงานเสร็จ
        Destroy(gameObject);
    }
}
