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

    [Header("Animation Settings")]
    [Tooltip("จุดที่จะไปโผล่บนหัวผู้เล่น (Local Offset)")]
    public Vector3 headOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("ความเร็วในการลอยขึ้น")]
    public float floatSpeed = 1f;
    [Tooltip("ความเร็วในการหดเล็กลงจนหายไป")]
    public float shrinkSpeed = 2f;

    private bool isActivated = false;

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
        PlayVoice(playerTransform.position);
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

        // ไปเกาะบนหัว Player
        transform.SetParent(playerTransform);
        transform.localPosition = headOffset;
        transform.localRotation = Quaternion.identity;

        isActivated = true;
    }

    private void Update()
    {
        if (isActivated)
        {
            // ลอยขึ้นเรื่อยๆ
            transform.Translate(Vector3.up * floatSpeed * Time.deltaTime, Space.World);

            // ค่อยๆ หดเล็กลง
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, shrinkSpeed * Time.deltaTime);

            // ถ้าหดจนมองไม่เห็นแล้ว ให้ทำลายทิ้ง
            if (transform.localScale.sqrMagnitude < 0.001f)
            {
                Destroy(gameObject);
            }
        }
    }
}
