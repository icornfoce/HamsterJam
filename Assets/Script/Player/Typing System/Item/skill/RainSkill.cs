using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Rain: สร้างพื้นที่ฝนตกลงมาจากฟ้า ทำดาเมจศัตรูที่อยู่ในวงซ้ำๆ + ชะลอความเร็ว
/// แนบสคริปต์นี้ไว้ที่ Prefab ของ Skill Rain แล้วลากใส่ช่อง "Item Skill" ใน ItemData
/// </summary>
public class RainSkill : BaseItemSkill
{
    [Header("Rain Zone Settings")]
    [Tooltip("รัศมีของพื้นที่ฝนตก")]
    public float rainRadius = 8f;

    [Tooltip("ระยะเวลาที่ฝนตก (วินาที)")]
    public float duration = 5f;

    [Tooltip("ดาเมจต่อครั้ง")]
    public int damagePerTick = 5;

    [Tooltip("ทำดาเมจทุกๆ กี่วินาที")]
    public float tickInterval = 0.5f;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ที่จะชะลอศัตรู (0.3 = ช้าลง 30%)")]
    [Range(0f, 1f)]
    public float slowPercent = 0.3f;

    [Header("Visual / Audio")]
    [Tooltip("VFX ฝนตก (จะถูกสร้างลอยอยู่เหนือจุดที่ปล่อย Skill)")]
    public GameObject rainVFXPrefab;
    [Tooltip("เสียงฝนตก")]
    public AudioClip rainSFX;

    private float timer;
    private float tickTimer;
    private Vector3 rainCenter;
    private bool isActive = false;
    private AudioSource loopAudio;

    // เก็บศัตรูที่ถูกชะลออยู่ เพื่อคืนค่าเมื่อฝนหยุด
    private Dictionary<NavMeshAgent, float> slowedEnemies = new Dictionary<NavMeshAgent, float>();

    public override void Activate(Transform playerTransform)
    {
        // ตั้งจุดศูนย์กลางฝนไว้ตรงตำแหน่ง Player ตอนปล่อย Skill (ฝนจะตกอยู่กับที่)
        rainCenter = playerTransform.position;
        transform.position = rainCenter;

        timer = duration;
        tickTimer = 0f;
        isActive = true;

        // สร้าง VFX ฝนตก
        if (rainVFXPrefab != null)
        {
            GameObject vfx = Instantiate(rainVFXPrefab, rainCenter + Vector3.up * 5f, Quaternion.identity, transform);
            Destroy(vfx, duration + 1f);
        }

        // เล่นเสียงฝน (Loop)
        if (rainSFX != null)
        {
            loopAudio = gameObject.AddComponent<AudioSource>();
            loopAudio.clip = rainSFX;
            loopAudio.loop = true;
            loopAudio.spatialBlend = 1f; // 3D sound
            loopAudio.Play();
        }

        Debug.Log($"<color=#4488FF>[RainSkill] ฝนตก! รัศมี: {rainRadius}, ดาเมจ: {damagePerTick}/ทุกๆ {tickInterval}วิ, ระยะเวลา: {duration} วินาที</color>");
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        // ทำดาเมจทุกๆ tickInterval วินาที
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            DamageEnemiesInZone();
        }

        // หมดเวลา → หยุดฝน
        if (timer <= 0f)
        {
            StopRain();
        }
    }

    private void DamageEnemiesInZone()
    {
        // หาศัตรูทั้งหมดในรัศมี
        Collider[] hits = Physics.OverlapSphere(rainCenter, rainRadius);

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            // ทำดาเมจ
            col.SendMessage("TakeDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);

            // ชะลอศัตรู
            NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
            if (agent != null && !slowedEnemies.ContainsKey(agent))
            {
                slowedEnemies[agent] = agent.speed; // เก็บค่าความเร็วเดิม
                agent.speed *= (1f - slowPercent);
            }
        }
    }

    private void StopRain()
    {
        isActive = false;

        // คืนค่าความเร็วให้ศัตรูทุกตัวที่ถูกชะลอ
        foreach (var pair in slowedEnemies)
        {
            if (pair.Key != null)
            {
                pair.Key.speed = pair.Value;
            }
        }
        slowedEnemies.Clear();

        // หยุดเสียง
        if (loopAudio != null)
        {
            loopAudio.Stop();
        }

        Debug.Log("<color=#4488FF>[RainSkill] ฝนหยุดตกแล้ว!</color>");
        Destroy(gameObject, 0.5f);
    }

    // แสดงรัศมีฝนใน Scene View เพื่อ Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(isActive ? rainCenter : transform.position, rainRadius);
    }
}
