using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ใส่ RawImage หลอดเลือด (อยากให้ลดซ้ายไปขวา ต้องปรับ Pivot X เป็น 1)")]
    public RawImage healthRawImage;
    
    [Tooltip("ใส่ Image หลอดเลือดสีฟ้า (ตั้ง Image Type เป็น Filled)")]
    public Image healthFillImage; 
    public Slider healthSlider; 
    public TextMeshProUGUI healthText; 

    private float maxRawWidth;
    private Rect originalUV;

    void Awake()
    {
        // ใช้ Awake เพื่อให้เก็บค่าเสร็จก่อนที่ PlayerHealth จะส่งค่าเลือดตอนเริ่มเกมมาให้
        if (healthRawImage != null)
        {
            maxRawWidth = healthRawImage.rectTransform.sizeDelta.x;
            originalUV = healthRawImage.uvRect;
        }
    }

    /// <summary>
    /// ฟังก์ชันนี้จะถูกเรียกอัตโนมัติเมื่อเลือดเปลี่ยน
    /// </summary>
    public void UpdateHealthBar(float normalizedHealth)
    {
        // 1. อัปเดต RawImage (ลดจากซ้ายไปขวา)
        if (healthRawImage != null)
        {
            // หดความกว้างของกล่อง UI
            healthRawImage.rectTransform.sizeDelta = new Vector2(maxRawWidth * normalizedHealth, healthRawImage.rectTransform.sizeDelta.y);
            
            // เลื่อนภาพด้านใน เพื่อให้มันโดนตัดจากฝั่งซ้ายแทน
            float newXOffset = originalUV.x + (originalUV.width * (1f - normalizedHealth));
            healthRawImage.uvRect = new Rect(newXOffset, originalUV.y, originalUV.width * normalizedHealth, originalUV.height);
        }

        // 2. อัปเดต Image Fill
        if (healthFillImage != null)
        {
            // ถ้าอยากให้ Image ลดจากซ้ายไปขวา ต้องไปตั้งใน Unity: Fill Origin = Right
            healthFillImage.fillAmount = normalizedHealth;
        }

        // 3. อัปเดต Slider
        if (healthSlider != null)
        {
            healthSlider.value = normalizedHealth;
        }

        // 4. อัปเดตตัวเลข Text
        if (healthText != null)
        {
            int hpNumber = Mathf.RoundToInt(normalizedHealth * 100f);
            healthText.text = hpNumber.ToString();
        }
    }
}
