using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ใส่ RawImage หลอดเลือด (ระบบจะยึดติดฝั่งซ้ายให้อัตโนมัติ)")]
    public RawImage healthRawImage;
    
    [Tooltip("ใส่ Image หลอดเลือดสีฟ้า")]
    public Image healthFillImage; 
    public Slider healthSlider; 
    public TextMeshProUGUI healthText; 

    [Header("Animation Settings")]
    [Tooltip("ความเร็วในการเลื่อนหลอดเลือด (ยิ่งเยอะยิ่งเร็ว)")]
    public float animationSpeed = 5f;

    private float maxRawWidth;
    private Rect originalUV;
    private Vector2 originalAnchoredPosition;

    private float targetHealth = 1f;
    private float currentVisualHealth = 1f;

    void Awake()
    {
        if (healthRawImage != null)
        {
            maxRawWidth = healthRawImage.rectTransform.sizeDelta.x;
            originalUV = healthRawImage.uvRect;
            originalAnchoredPosition = healthRawImage.rectTransform.anchoredPosition;
        }
    }

    public void UpdateHealthBar(float normalizedHealth)
    {
        // ตั้งเป้าหมายให้หลอดเลือดรู้ว่าต้องลดไปถึงจุดไหน
        targetHealth = normalizedHealth;
    }

    void Update()
    {
        // ทำแอนิเมชันให้หลอดเลือดค่อยๆ ลดลงอย่างนุ่มนวล (Lerp)
        if (Mathf.Abs(currentVisualHealth - targetHealth) > 0.001f)
        {
            currentVisualHealth = Mathf.Lerp(currentVisualHealth, targetHealth, Time.deltaTime * animationSpeed);
            ApplyVisualUpdate(currentVisualHealth);
        }
        else if (currentVisualHealth != targetHealth)
        {
            // ปัดเศษให้เท่ากันพอดีเมื่อใกล้เคียงมากๆ
            currentVisualHealth = targetHealth;
            ApplyVisualUpdate(currentVisualHealth);
        }
    }

    private void ApplyVisualUpdate(float healthValue)
    {
        // 1. อัปเดต RawImage
        if (healthRawImage != null)
        {
            float newWidth = maxRawWidth * healthValue;
            
            healthRawImage.rectTransform.sizeDelta = new Vector2(newWidth, healthRawImage.rectTransform.sizeDelta.y);
            
            float pivotX = healthRawImage.rectTransform.pivot.x;
            float shiftX = (maxRawWidth - newWidth) * pivotX;
            healthRawImage.rectTransform.anchoredPosition = new Vector2(originalAnchoredPosition.x - shiftX, originalAnchoredPosition.y);
            
            // ปิดการทำงานของคำสั่งตัดภาพ (uvRect)
            // พอเราไม่ตัดภาพ มันก็จะใช้วิธี "บีบหด" รูปภาพแทน ทำให้ปลายโค้งๆ ของรูปแคปซูลยังอยู่เหมือนเดิม
            healthRawImage.uvRect = originalUV; 
        }

        // 2. อัปเดต Image Fill
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = healthValue;
        }

        // 3. อัปเดต Slider
        if (healthSlider != null)
        {
            healthSlider.value = healthValue;
        }

        // 4. อัปเดต Text
        if (healthText != null)
        {
            int hpNumber = Mathf.RoundToInt(healthValue * 100f);
            healthText.text = hpNumber.ToString();
        }
    }
}
