using UnityEngine;

/// <summary>
/// ลูกหิมะแต่ละลูกที่ถูกยิงออกจาก SnowBallShotgunSkill
/// แปะไว้ที่ Prefab ลูกหิมะ หรือระบบจะ AddComponent ให้อัตโนมัติ
/// </summary>
public class SnowballPellet : MonoBehaviour
{
    private float speed;
    private int damage;
    private float slowPercent;
    private float slowDuration;
    private GameObject hitVFXPrefab;
    private AudioClip hitSFX;
    private bool isSetup = false;

    public void Setup(float speed, int damage, float lifetime, float slowPercent, float slowDuration, GameObject hitVFX, AudioClip hitSFX)
    {
        this.speed = speed;
        this.damage = damage;
        this.slowPercent = slowPercent;
        this.slowDuration = slowDuration;
        this.hitVFXPrefab = hitVFX;
        this.hitSFX = hitSFX;
        isSetup = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isSetup) return;

        float moveDistance = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.2f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                ApplySlow(hit.collider.gameObject);
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
            else if (!hit.collider.CompareTag("Player"))
            {
                SpawnHitEffect(hit.point);
                Destroy(gameObject);
                return;
            }
        }

        transform.Translate(Vector3.forward * moveDistance, Space.Self);
    }

    private void ApplySlow(GameObject enemy)
    {
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            SlowEffect existing = enemy.GetComponent<SlowEffect>();
            if (existing != null)
                existing.RefreshSlow(slowPercent, slowDuration);
            else
            {
                SlowEffect slow = enemy.AddComponent<SlowEffect>();
                slow.Setup(agent, slowPercent, slowDuration);
            }
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(hitVFXPrefab, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, position);
        }
    }
}
