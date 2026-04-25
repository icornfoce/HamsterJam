using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Fan: พัดศัตรูที่อยู่ข้างหน้าให้กระเด็นออกไป
/// </summary>
public class FanSkill : BaseItemSkill
{
    [Header("Fan Settings")]
    [Tooltip("รัศมีที่พัดได้")]
    public float pushRadius = 10f;

    [Tooltip("มุมกว้างของลม (องศา)")]
    public float pushAngle = 180f;

    [Tooltip("แรงผลัก")]
    public float pushForce = 25f;

    [Tooltip("ระยะเวลาที่ลมพัด (วินาที)")]
    public float pushDuration = 0.8f;

    [Header("Visual / Audio")]
    public GameObject windVFXPrefab;
    public AudioClip windSFX;

    private float timer = -1f;
    private List<PushedEnemy> pushedEnemies = new List<PushedEnemy>();

    private class PushedEnemy
    {
        public Transform transform;
        public NavMeshAgent agent;
        public Vector3 direction;
    }

    public override void Activate(Transform playerTransform)
    {
        timer = pushDuration;

        if (windSFX != null)
            AudioSource.PlayClipAtPoint(windSFX, playerTransform.position);

        if (windVFXPrefab != null)
        {
            GameObject vfx = Instantiate(windVFXPrefab, playerTransform.position, playerTransform.rotation);
            Destroy(vfx, pushDuration + 1f);
        }

        // หาศัตรูทั้งหมดในฉากจาก Component (ไม่ต้องพึ่ง Tag)
        List<Transform> allEnemies = new List<Transform>();

        foreach (var e in FindObjectsOfType<Enemy>())
            allEnemies.Add(e.transform);
        foreach (var e in FindObjectsOfType<Rangeenemy>())
            allEnemies.Add(e.transform);
        foreach (var e in FindObjectsOfType<Furnace>())
            allEnemies.Add(e.transform);

        Debug.Log($"[FanSkill] พบศัตรูในฉากทั้งหมด {allEnemies.Count} ตัว");

        Vector3 playerPos = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;

        foreach (Transform enemyTr in allEnemies)
        {
            float dist = Vector3.Distance(playerPos, enemyTr.position);
            if (dist > pushRadius) continue;

            Vector3 dirToEnemy = (enemyTr.position - playerPos).normalized;
            float angle = Vector3.Angle(playerForward, dirToEnemy);

            if (angle > pushAngle * 0.5f) continue;

            NavMeshAgent agent = enemyTr.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            pushedEnemies.Add(new PushedEnemy
            {
                transform = enemyTr,
                agent = agent,
                direction = dirToEnemy
            });

            Debug.Log($"[FanSkill] กำลังพัด: {enemyTr.name} (ระยะ: {dist:F1}, มุม: {angle:F1}°)");
        }

        Debug.Log($"<color=green>[FanSkill] พัดศัตรูได้ {pushedEnemies.Count} ตัว!</color>");

        if (pushedEnemies.Count == 0)
            Destroy(gameObject, 0.5f);
    }

    private void Update()
    {
        if (timer < 0f) return;

        timer -= Time.deltaTime;

        for (int i = pushedEnemies.Count - 1; i >= 0; i--)
        {
            PushedEnemy enemy = pushedEnemies[i];
            if (enemy.transform == null)
            {
                pushedEnemies.RemoveAt(i);
                continue;
            }

            enemy.transform.position += enemy.direction * pushForce * Time.deltaTime;
        }

        if (timer <= 0f)
        {
            // เปิด NavMeshAgent คืนให้ศัตรู
            foreach (PushedEnemy enemy in pushedEnemies)
            {
                if (enemy.agent != null && enemy.transform != null)
                    enemy.agent.enabled = true;
            }
            
            pushedEnemies.Clear();
            Destroy(gameObject, 0.2f);
        }
    }
}
