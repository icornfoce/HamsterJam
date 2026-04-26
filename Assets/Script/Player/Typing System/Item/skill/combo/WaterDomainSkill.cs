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

    [Header("Visual / Audio")]
    public GameObject explosionVFXPrefab;
    public AudioClip explosionSFX;

    private List<PushedEnemy> pushedEnemies = new List<PushedEnemy>();
    private float pushTimer = -1f;

    private class PushedEnemy
    {
        public Transform transform;
        public NavMeshAgent agent;
        public Vector3 direction;
    }

    public override void Activate(Transform playerTransform)
    {
        Vector3 center = playerTransform.position;
        transform.position = center;

        PlayVoice(center);

        // เล่นเสียงระเบิด
        if (explosionSFX != null)
            AudioSource.PlayClipAtPoint(explosionSFX, center);

        // สร้าง VFX คลื่นน้ำ
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, center, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // หาศัตรูทั้งหมดในรัศมี
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius);

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            // ทำดาเมจ
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // คำนวณทิศทางผลัก (จากจุดศูนย์กลางไปหาศัตรู)
            Vector3 dir = (col.transform.position - center).normalized;
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

        if (pushedEnemies.Count == 0)
            Destroy(gameObject, 0.5f);
    }

    private void Update()
    {
        if (pushTimer < 0f) return;

        pushTimer -= Time.deltaTime;

        // ผลักศัตรูออกไปทุกเฟรม
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
        if (pushTimer <= 0f)
        {
            foreach (var enemy in pushedEnemies)
            {
                if (enemy.agent != null && enemy.transform != null)
                    enemy.agent.enabled = true;
            }
            pushedEnemies.Clear();
            Destroy(gameObject, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
