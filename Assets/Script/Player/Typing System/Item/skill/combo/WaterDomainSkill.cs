using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Water Domain: ระเบิดคลื่นน้ำออกมารอบตัวผู้เล่น
/// ทำดาเมจ + ผลักศัตรูกระเด็นออกไปทุกทิศทาง
/// ผลลัพธ์จากการรวม Water + Water
/// </summary>
public class WaterDomainSkill : BaseItemSkill
{
    [Header("Explosion Settings")]
    [Tooltip("รัศมีของคลื่นน้ำ")]
    public float explosionRadius = 12f;

    [Tooltip("ดาเมจที่ทำใส่ศัตรู")]
    public int damage = 40;

    [Tooltip("แรงผลักศัตรูกระเด็น")]
    public float pushForce = 30f;

    [Tooltip("ระยะเวลาที่ศัตรูถูกผลัก (วินาที)")]
    public float pushDuration = 0.8f;

    [Tooltip("ระยะเวลารอก่อนที่คลื่นน้ำจะทำดาเมจ (วินาที)")]
    public float damageDelay = 0.5f;

    [Tooltip("ความเร็วในการหดเล็กลงจนหายไป")]
    public float shrinkSpeed = 1f;

    [Tooltip("ระยะเวลาที่จะรอให้ Effect ของสกิลนี้เล่นจนจบก่อนค่อยหายไป (วินาที)")]
    public float destroyDelay = 3f;

    [Header("Visual / Audio")]
    public GameObject explosionVFXPrefab;
    public AudioClip explosionSFX;

    private List<PushedEnemy> pushedEnemies = new List<PushedEnemy>();
    private float pushTimer = -1f;
    private float damageTimer = 0f;
    private bool hasDealtDamage = false;
    private bool isActivated = false;
    private Vector3 centerPos;

    private class PushedEnemy
    {
        public Transform transform;
        public NavMeshAgent agent;
        public Vector3 direction;
    }

    public override void Activate(Transform playerTransform)
    {
        centerPos = playerTransform.position;
        transform.position = centerPos;

        PlayVoice(centerPos);

        damageTimer = damageDelay;
        isActivated = true;

        // ถ้าไม่ได้ตั้งค่า Delay ให้ระเบิดทันที
        if (damageDelay <= 0f)
        {
            Explode();
        }
    }

    private void Update()
    {
        if (!isActivated) return;

        // เฟส 1: รอก่อนทำดาเมจ
        if (!hasDealtDamage)
        {
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0f)
            {
                Explode();
            }
            return; // ยังไม่ผลักใครจนกว่าจะระเบิด
        }

        // เฟส 2: ค่อยๆ หดเล็กลง (ถ้าเปิดใช้งาน)
        if (shrinkSpeed > 0f)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, shrinkSpeed * Time.deltaTime);
        }

        // เฟส 3: จัดการผลักศัตรู
        if (pushTimer >= 0f)
        {
            pushTimer -= Time.deltaTime;

            for (int i = pushedEnemies.Count - 1; i >= 0; i--)
            {
                if (pushedEnemies[i].transform == null)
                {
                    pushedEnemies.RemoveAt(i);
                    continue;
                }
                pushedEnemies[i].transform.position += pushedEnemies[i].direction * pushForce * Time.deltaTime;
            }

            // หมดเวลาผลัก → คืน NavMeshAgent
            if (pushTimer < 0f)
            {
                foreach (var enemy in pushedEnemies)
                {
                    if (enemy.agent != null && enemy.transform != null)
                        enemy.agent.enabled = true;
                }
                pushedEnemies.Clear();
            }
        }
    }

    private void Explode()
    {
        hasDealtDamage = true;

        // เล่นเสียงระเบิด
        if (explosionSFX != null)
            AudioSource.PlayClipAtPoint(explosionSFX, centerPos);

        // สร้าง VFX คลื่นน้ำ
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, centerPos, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // หาศัตรูทั้งหมดในรัศมี
        Collider[] hits = Physics.OverlapSphere(centerPos, explosionRadius);

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            // ทำดาเมจ
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // คำนวณทิศทางผลัก (จากจุดศูนย์กลางไปหาศัตรู)
            Vector3 dir = (col.transform.position - centerPos).normalized;
            if (dir == Vector3.zero) dir = Vector3.forward;

            // ปิด NavMeshAgent ชั่วคราวเพื่อให้ศัตรูกระเด็นได้
            NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            pushedEnemies.Add(new PushedEnemy
            {
                transform = col.transform,
                agent = agent,
                direction = dir
            });
        }

        pushTimer = pushDuration;

        Debug.Log($"<color=#0088FF>[WaterDomain] ระเบิดคลื่นน้ำ! โดนศัตรู {pushedEnemies.Count} ตัว, ดาเมจ: {damage}</color>");

        // สั่งลบตัวเองเมื่อหมดเวลาที่ตั้งไว้ เพื่อให้ Effect/Animation เล่นจนจบ
        Destroy(gameObject, destroyDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
