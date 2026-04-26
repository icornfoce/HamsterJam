using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Hail Storm: ลูกเห็บตกจากฟ้าถล่มศัตรูในพื้นที่กว้าง
/// ลูกเห็บแต่ละลูกตกแบบสุ่มตำแหน่งในรัศมี ทำดาเมจสูงและชะลอศัตรู
/// </summary>
public class HailStormSkill : BaseItemSkill
{
    [Header("Hail Storm Settings")]
    [Tooltip("รัศมีของพื้นที่ลูกเห็บตก")]
    public float stormRadius = 10f;

    [Tooltip("ระยะเวลาที่พายุคงอยู่ (วินาที)")]
    public float duration = 4f;

    [Tooltip("จำนวนลูกเห็บที่ตกต่อวินาที")]
    public int hailPerSecond = 8;

    [Header("Hail Damage")]
    [Tooltip("ดาเมจต่อลูก")]
    public int damagePerHail = 10;

    [Tooltip("รัศมีระเบิดของลูกเห็บแต่ละลูก (Splash)")]
    public float hailSplashRadius = 3f;

    [Header("Slow Effect")]
    [Tooltip("เปอร์เซ็นต์ชะลอศัตรูที่โดน")]
    [Range(0f, 1f)]
    public float slowPercent = 0.4f;

    [Tooltip("ระยะเวลาที่ถูกชะลอ (วินาที)")]
    public float slowDuration = 2f;

    [Header("Hail Visuals")]
    [Tooltip("Prefab ลูกเห็บ (ถ้าไม่มีจะใช้ Raycast แทน)")]
    public GameObject hailPrefab;

    [Tooltip("ความสูงที่ลูกเห็บตกลงมา")]
    public float spawnHeight = 15f;

    [Tooltip("ความเร็วลูกเห็บตก")]
    public float fallSpeed = 30f;

    [Header("Audio")]
    public AudioClip stormStartSFX;
    public AudioClip hailHitSFX;
    public GameObject stormVFXPrefab;

    private Vector3 stormCenter;
    private float timer;
    private float spawnTimer;
    private float spawnInterval;
    private bool isActive = false;
    private AudioSource loopAudio;

    // เก็บลูกเห็บที่กำลังตก
    private List<FallingHail> fallingHails = new List<FallingHail>();

    private class FallingHail
    {
        public GameObject obj;
        public Vector3 targetPos;
        public bool hasHit;
    }

    public override void Activate(Transform playerTransform)
    {
        stormCenter = playerTransform.position;
        transform.position = stormCenter;

        timer = duration;
        spawnInterval = 1f / hailPerSecond;
        spawnTimer = 0f;
        isActive = true;

        PlayVoice(playerTransform.position);

        // เสียงเริ่มพายุ
        if (stormStartSFX != null)
            AudioSource.PlayClipAtPoint(stormStartSFX, stormCenter);

        // VFX พายุ
        if (stormVFXPrefab != null)
        {
            GameObject vfx = Instantiate(stormVFXPrefab, stormCenter + Vector3.up * spawnHeight * 0.5f, Quaternion.identity, transform);
            Destroy(vfx, duration + 2f);
        }

        Debug.Log($"<color=#AADDFF>[HailStorm] พายุลูกเห็บ! รัศมี: {stormRadius}, ดาเมจ/ลูก: {damagePerHail}, {hailPerSecond} ลูก/วินาที</color>");
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        // สร้างลูกเห็บใหม่
        if (spawnTimer <= 0f && timer > 0f)
        {
            spawnTimer = spawnInterval;
            SpawnHail();
        }

        // อัปเดตลูกเห็บที่กำลังตก
        UpdateFallingHails();

        // หมดเวลา + ลูกเห็บตกหมดแล้ว
        if (timer <= 0f && fallingHails.Count == 0)
        {
            isActive = false;
            Debug.Log("<color=#AADDFF>[HailStorm] พายุลูกเห็บหยุดแล้ว!</color>");
            Destroy(gameObject, 0.5f);
        }
    }

    private void SpawnHail()
    {
        // สุ่มตำแหน่งในรัศมี
        Vector2 randomCircle = Random.insideUnitCircle * stormRadius;
        Vector3 targetPos = stormCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;

        FallingHail hail = new FallingHail
        {
            targetPos = targetPos,
            hasHit = false
        };

        // สร้างลูกเห็บ (ถ้ามี Prefab)
        if (hailPrefab != null)
        {
            hail.obj = Instantiate(hailPrefab, spawnPos, Quaternion.identity);
        }

        fallingHails.Add(hail);
    }

    private void UpdateFallingHails()
    {
        for (int i = fallingHails.Count - 1; i >= 0; i--)
        {
            FallingHail hail = fallingHails[i];
            if (hail.hasHit)
            {
                fallingHails.RemoveAt(i);
                continue;
            }

            // เลื่อนลูกเห็บลงมา
            if (hail.obj != null)
            {
                hail.obj.transform.position += Vector3.down * fallSpeed * Time.deltaTime;

                // ถึงพื้นแล้ว
                if (hail.obj.transform.position.y <= hail.targetPos.y)
                {
                    HailImpact(hail.targetPos);
                    Destroy(hail.obj);
                    hail.hasHit = true;
                }
            }
            else
            {
                // ถ้าไม่มี Prefab ให้ระเบิดทันที
                HailImpact(hail.targetPos);
                hail.hasHit = true;
            }
        }
    }

    private void HailImpact(Vector3 position)
    {
        // เสียงตอนลูกเห็บกระทบ
        if (hailHitSFX != null)
            AudioSource.PlayClipAtPoint(hailHitSFX, position, 0.5f);

        // ทำดาเมจ + ชะลอศัตรูในรัศมี Splash
        Collider[] hits = Physics.OverlapSphere(position, hailSplashRadius);

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            col.SendMessage("TakeDamage", damagePerHail, SendMessageOptions.DontRequireReceiver);

            // ชะลอ
            UnityEngine.AI.NavMeshAgent agent = col.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                SlowEffect existingSlow = col.GetComponent<SlowEffect>();
                if (existingSlow != null)
                {
                    existingSlow.RefreshSlow(slowPercent, slowDuration);
                }
                else
                {
                    SlowEffect slow = col.gameObject.AddComponent<SlowEffect>();
                    slow.Setup(agent, slowPercent, slowDuration);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.85f, 1f, 0.3f);
        Gizmos.DrawWireSphere(isActive ? stormCenter : transform.position, stormRadius);
    }
}
