using UnityEngine;

/// <summary>
/// Skill ของ Ice Cream: ฮีล HP ให้ผู้เล่น
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill Ice Cream แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class IceCreamSkill : BaseItemSkill
{
    [Header("Heal Settings")]
    [Tooltip("จำนวน HP ที่จะฮีลให้ผู้เล่น")]
    public float healAmount = 30f;

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
            Debug.LogWarning("[IceCreamSkill] ไม่พบ PlayerHealth! Skill ไม่ทำงาน");
            Destroy(gameObject);
            return;
        }

        playerHealth.Heal(healAmount);
        Debug.Log($"<color=cyan>[IceCreamSkill] ฮีลผู้เล่น +{healAmount} HP!</color>");

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
