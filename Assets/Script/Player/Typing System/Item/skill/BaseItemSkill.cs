using UnityEngine;

/// <summary>
/// คลาสฐานสำหรับ Skill ของไอเทมทุกชนิด
/// นำไปสร้าง Script ใหม่และ Inherit จากคลาสนี้
/// จากนั้นแนบ Script นั้นไว้ที่ Prefab ของ Skill
/// </summary>
public abstract class BaseItemSkill : MonoBehaviour
{
    [Header("Voice / Announcement")]
    [Tooltip("เสียงพูดตอนปล่อย Skill (เช่น ตะโกนชื่อท่า)")]
    public AudioClip voiceClip;
    [Tooltip("ความดังของเสียงพูด")]
    [Range(0f, 1f)]
    public float voiceVolume = 1f;

    /// <summary>
    /// เรียกใช้ทันทีเมื่อ Skill ถูกปล่อยออกมา
    /// ทุก Skill ต้อง Override ฟังก์ชันนี้
    /// </summary>
    /// <param name="playerTransform">Transform ของผู้เล่น (ใช้ดึง ตำแหน่ง/ทิศทาง/PlayerHealth ได้)</param>
    public abstract void Activate(Transform playerTransform);

    /// <summary>
    /// เล่นเสียงพูดตอนใช้ Skill (เรียกจาก Activate ของแต่ละ Skill)
    /// </summary>
    protected void PlayVoice(Vector3 position)
    {
        if (voiceClip != null)
        {
            AudioSource.PlayClipAtPoint(voiceClip, position, voiceVolume);
        }
    }
}
