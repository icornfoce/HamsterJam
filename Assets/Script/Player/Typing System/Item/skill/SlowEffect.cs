using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Component ชั่วคราวที่ติดที่ตัวศัตรู เพื่อชะลอความเร็วชั่วคราว แล้วคืนค่าอัตโนมัติเมื่อหมดเวลา
/// ถูกสร้างโดย DryIceSkill อัตโนมัติ (ไม่ต้องแปะเองใน Inspector)
/// </summary>
public class SlowEffect : MonoBehaviour
{
    private NavMeshAgent agent;
    private float originalSpeed;
    private float timer;

    /// <summary>
    /// ตั้งค่า Slow Effect ครั้งแรก
    /// </summary>
    public void Setup(NavMeshAgent navAgent, float slowPercent, float duration)
    {
        agent = navAgent;
        originalSpeed = agent.speed;
        timer = duration;

        // ลดความเร็วลงตาม slowPercent
        agent.speed = originalSpeed * (1f - slowPercent);

        Debug.Log($"<color=#88DDFF>[SlowEffect] {gameObject.name} ถูกชะลอ! ความเร็วเดิม: {originalSpeed} → {agent.speed} เป็นเวลา {duration} วินาที</color>");
    }

    /// <summary>
    /// รีเซ็ต Timer เมื่อโดนซ้ำ (ไม่ซ้อนความช้า)
    /// </summary>
    public void RefreshSlow(float slowPercent, float duration)
    {
        timer = duration;
        
        // คืนค่าเดิมก่อน แล้วค่อยลดใหม่
        if (agent != null)
        {
            agent.speed = originalSpeed * (1f - slowPercent);
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // คืนค่าความเร็วปกติ
            if (agent != null)
            {
                agent.speed = originalSpeed;
                Debug.Log($"<color=#88DDFF>[SlowEffect] {gameObject.name} หายจากการชะลอแล้ว! ความเร็วกลับเป็น {originalSpeed}</color>");
            }

            // ลบ Component ตัวเองทิ้ง
            Destroy(this);
        }
    }
}
