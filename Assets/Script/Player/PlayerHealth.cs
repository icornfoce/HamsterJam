using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Events")]
    // Event that passes the normalized health (0.0 to 1.0) useful for UI health bars
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    [Header("Vignette Effects")]
    [Tooltip("ลาก Q_Vignette_Base (ที่ตั้งค่าสีแดงไว้) มาใส่ช่องนี้")]
    public Q_Vignette_Base damageVignette;
    [Tooltip("ความเร็วในการเข้มขึ้น")]
    public float fadeInSpeed = 5f;
    [Tooltip("ความเร็วในการจางลง")]
    public float fadeOutSpeed = 2f;
    [Tooltip("ความเข้มสูงสุดตอนขอบแดง (0-1)")]
    public float maxAlpha = 1f;

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private bool isDead = false;

    void Update()
    {
        // จัดการความเข้มของ Q Vignette ตอนโดนตี
        if (damageVignette != null)
        {
            float newVal = Mathf.MoveTowards(
                currentAlpha, 
                targetAlpha, 
                (targetAlpha > 0 ? fadeInSpeed : fadeOutSpeed) * Time.deltaTime
            );

            currentAlpha = newVal;
            SetVignetteAlpha(currentAlpha);

            // เมื่อเข้มเต็มที่แล้ว ให้ตั้งเป้าหมายเป็น 0 เพื่อให้จางกลับ
            if (newVal >= maxAlpha && targetAlpha > 0f)
            {
                targetAlpha = 0f;
            }

            // เปิด/ปิด GameObject 
            if (newVal > 0 && !damageVignette.gameObject.activeSelf)
            {
                damageVignette.gameObject.SetActive(true);
            }
            else if (newVal <= 0 && damageVignette.gameObject.activeSelf)
            {
                damageVignette.gameObject.SetActive(false);
            }
        }
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (damageVignette.cornerImages == null) return;
        for (int i = 0; i < damageVignette.cornerImages.Length; i++)
        {
            if (damageVignette.cornerImages[i] != null)
            {
                Color c = damageVignette.cornerImages[i].color;
                c.a = alpha;
                damageVignette.cornerImages[i].color = c;
            }
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthNormalized());

        // กำหนดโปร่งใสเริ่มต้น
        if (damageVignette != null)
        {
            currentAlpha = 0f;
            SetVignetteAlpha(0f);
            damageVignette.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(GetHealthNormalized());

        if (damageVignette != null)
        {
            targetAlpha = maxAlpha;
        }

        Debug.Log($"Player took {damageAmount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(GetHealthNormalized());

        // เมื่อฮีล ให้ขอบจอแดง (Damage Vignette) หายไปทันที
        if (damageVignette != null)
        {
            targetAlpha = 0f;
            currentAlpha = 0f;
            SetVignetteAlpha(0f);
            damageVignette.gameObject.SetActive(false);
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");
        OnDeath?.Invoke();
        
        // Play Death Cutscene
        if (VideoManager.Instance != null)
        {
            VideoManager.Instance.PlayPlayerDeath(() => {
                // You might want to reload scene or show Game Over UI here
                Debug.Log("Player death cutscene finished.");
            });
        }

        // Disable movement
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    public float GetHealthNormalized()
    {
        return currentHealth / maxHealth;
    }
}
