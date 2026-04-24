using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText; // ใช้ TextMeshPro

    /// <summary>
    /// ฟังก์ชันนี้จะถูกเรียกอัตโนมัติเมื่อเลือดเปลี่ยน
    /// </summary>
    public void UpdateHealthBar(float normalizedHealth)
    {
        // 1. อัปเดตหลอดเลือด Slider (ถ้ามี)
        if (healthSlider != null)
        {
            healthSlider.value = normalizedHealth;
        }

        // 2. อัปเดตตัวเลข Text (ถ้ามี)
        if (healthText != null)
        {
            // แปลงค่า 0.0 - 1.0 ให้กลายเป็น 0 - 100
            int hpNumber = Mathf.RoundToInt(normalizedHealth * 100f);
            healthText.text = "HP: " + hpNumber.ToString();
        }
    }
}
