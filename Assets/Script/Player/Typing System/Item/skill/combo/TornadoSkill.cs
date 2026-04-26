using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Skill ของ Tornado: สร้างพายุทอร์นาโดที่ดูดศัตรูเข้ามาหมุนวนแล้วเหวี่ยงออก
/// ผลลัพธ์จากการรวม Fan + Fan
/// </summary>
public class TornadoSkill : BaseItemSkill
{
    [Header("Tornado Settings")]
    [Tooltip("รัศมีที่ดูดศัตรูได้")]
    public float pullRadius = 14f;

    [Tooltip("ระยะเวลาที่พายุคงอยู่ (วินาที)")]
    public float duration = 4f;

    [Tooltip("ดาเมจต่อครั้ง")]
    public int damagePerTick = 6;

    [Tooltip("ทำดาเมจทุกๆ กี่วินาที")]
    public float tickInterval = 0.3f;

    [Header("Pull & Spin")]
    [Tooltip("แรงดูดเข้าหาจุดศูนย์กลาง")]
    public float pullForce = 8f;

    [Tooltip("แรงหมุนวน")]
    public float spinForce = 12f;

    [Tooltip("แรงยกขึ้น")]
    public float liftForce = 3f;

    [Header("Final Throw (เหวี่ยงตอนจบ)")]
    [Tooltip("แรงเหวี่ยงออกตอนพายุหยุด")]
    public float throwForce = 20f;

    [Tooltip("ระยะเวลาที่ศัตรูถูกเหวี่ยง (วินาที)")]
    public float throwDuration = 0.6f;

    [Header("Visual / Audio")]
    public GameObject tornadoVFXPrefab;
    public AudioClip tornadoSFX;

    private Vector3 tornadoCenter;
    private float timer;
    private float tickTimer;
    private bool isActive = false;
    private bool isThrowing = false;
    private float throwTimer;
    private AudioSource loopAudio;

    private List<CaughtEnemy> caughtEnemies = new List<CaughtEnemy>();

    private class CaughtEnemy
    {
        public Transform transform;
        public NavMeshAgent agent;
        public Vector3 throwDir;
    }

    public override void Activate(Transform playerTransform)
    {
        // พายุเกิดที่ด้านหน้า Player
        tornadoCenter = playerTransform.position + playerTransform.forward * 5f;
        transform.position = tornadoCenter;

        timer = duration;
        tickTimer = 0f;
        isActive = true;

        PlayVoice(playerTransform.position);

        // VFX
        if (tornadoVFXPrefab != null)
        {
            GameObject vfx = Instantiate(tornadoVFXPrefab, tornadoCenter, Quaternion.identity, transform);
            Destroy(vfx, duration + throwDuration + 1f);
        }

        // เสียง Loop
        if (tornadoSFX != null)
        {
            loopAudio = gameObject.AddComponent<AudioSource>();
            loopAudio.clip = tornadoSFX;
            loopAudio.loop = true;
            loopAudio.spatialBlend = 1f;
            loopAudio.Play();
        }

        Debug.Log($"<color=#00FF88>[Tornado] พายุทอร์นาโด! รัศมี: {pullRadius}, ดาเมจ: {damagePerTick}/ทุกๆ {tickInterval}วิ</color>");
    }

    private void Update()
    {
        if (!isActive) return;

        // ช่วงเหวี่ยงออก
        if (isThrowing)
        {
            UpdateThrow();
            return;
        }

        timer -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        // ดูด + หมุน + ดาเมจ
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            PullAndDamage();
        }

        // หมุนศัตรูที่ถูกจับทุกเฟรม
        SpinEnemies();

        // หมดเวลา → เหวี่ยงออก
        if (timer <= 0f)
        {
            StartThrow();
        }
    }

    private void PullAndDamage()
    {
        Collider[] hits = Physics.OverlapSphere(tornadoCenter, pullRadius);

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            // ทำดาเมจ
            col.SendMessage("TakeDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);

            // ถ้ายังไม่ได้จับ → จับเข้าพายุ
            bool alreadyCaught = false;
            foreach (var e in caughtEnemies)
            {
                if (e.transform == col.transform) { alreadyCaught = true; break; }
            }

            if (!alreadyCaught)
            {
                NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;

                caughtEnemies.Add(new CaughtEnemy
                {
                    transform = col.transform,
                    agent = agent,
                    throwDir = Vector3.zero
                });
            }
        }
    }

    private void SpinEnemies()
    {
        for (int i = caughtEnemies.Count - 1; i >= 0; i--)
        {
            CaughtEnemy enemy = caughtEnemies[i];
            if (enemy.transform == null)
            {
                caughtEnemies.RemoveAt(i);
                continue;
            }

            Vector3 toCenter = (tornadoCenter - enemy.transform.position);
            float dist = toCenter.magnitude;

            // ดูดเข้าหาจุดศูนย์กลาง
            Vector3 pullDir = toCenter.normalized * pullForce;

            // หมุนวนรอบจุดศูนย์กลาง (ใช้ Cross Product กับแกน Y)
            Vector3 spinDir = Vector3.Cross(Vector3.up, toCenter.normalized) * spinForce;

            // ยกขึ้น
            Vector3 lift = Vector3.up * liftForce;

            enemy.transform.position += (pullDir + spinDir + lift) * Time.deltaTime;

            // เก็บทิศทางเหวี่ยงไว้ (จากจุดศูนย์กลางออกไปหาศัตรู)
            if (dist > 0.1f)
                enemy.throwDir = -toCenter.normalized + Vector3.up * 0.5f;
        }
    }

    private void StartThrow()
    {
        isThrowing = true;
        throwTimer = throwDuration;

        Debug.Log($"<color=#00FF88>[Tornado] เหวี่ยงศัตรู {caughtEnemies.Count} ตัวออก!</color>");
    }

    private void UpdateThrow()
    {
        throwTimer -= Time.deltaTime;

        for (int i = caughtEnemies.Count - 1; i >= 0; i--)
        {
            CaughtEnemy enemy = caughtEnemies[i];
            if (enemy.transform == null)
            {
                caughtEnemies.RemoveAt(i);
                continue;
            }
            enemy.transform.position += enemy.throwDir * throwForce * Time.deltaTime;
        }

        if (throwTimer <= 0f)
        {
            // คืน NavMeshAgent
            foreach (var enemy in caughtEnemies)
            {
                if (enemy.agent != null && enemy.transform != null)
                    enemy.agent.enabled = true;
            }
            caughtEnemies.Clear();

            if (loopAudio != null) loopAudio.Stop();
            isActive = false;
            Destroy(gameObject, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(isActive ? tornadoCenter : transform.position, pullRadius);
    }
}
