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

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(GetHealthNormalized());
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke(GetHealthNormalized());
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
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");
        OnDeath?.Invoke();
        
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
