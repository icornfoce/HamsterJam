using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RawImage mainRawImage;
    [SerializeField] private RawImage chipRawImage; 
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    private float maxWidth;
    private float currentMainRatio = 1f;
    private float currentChipRatio = 1f;

    [Header("Settings")]
    [SerializeField] private string bossName = "FURNACE";
    [SerializeField] private float chipSpeed = 0.5f;
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine chipCoroutine;

    private void Awake()
    {
        // ค้นหา CanvasGroup อัตโนมัติถ้าไม่ได้ใส่มา
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0;

        // พยายามหา UI Components อัตโนมัติจากลูกๆ (Children) ถ้าไม่ได้ลากมาใส่
        if (mainRawImage == null || chipRawImage == null)
        {
            RawImage[] images = GetComponentsInChildren<RawImage>(true);
            foreach (var img in images)
            {
                if (mainRawImage == null && img.gameObject.name.ToLower().Contains("main")) mainRawImage = img;
                else if (chipRawImage == null && img.gameObject.name.ToLower().Contains("chip")) chipRawImage = img;
            }
            // Fallback: ถ้าหาตามชื่อไม่เจอ ให้หยิบตัวแรกๆ มาเลย
            if (mainRawImage == null && images.Length > 0) mainRawImage = images[0];
            if (chipRawImage == null && images.Length > 1) chipRawImage = images[1];
        }

        if (bossNameText == null) bossNameText = GetComponentInChildren<TextMeshProUGUI>(true);

        // ตั้งชื่อบอส
        if (bossNameText != null) bossNameText.text = bossName;

        // เก็บค่าความกว้างสูงสุด
        if (mainRawImage != null)
            maxWidth = mainRawImage.rectTransform.sizeDelta.x;
        else
            Debug.LogWarning("[BossHealthBar] ไม่พบ MainRawImage กรุณาตรวจสอบการตั้งค่าใน Inspector!");
    }

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.gameObject.SetActive(true);
        }
        StopAllCoroutines();
        StartCoroutine(Fade(1f));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(0f));
    }

    public void UpdateHealth(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);
        currentMainRatio = ratio;
        UpdateVisual(mainRawImage, currentMainRatio);

        if (chipCoroutine != null) StopCoroutine(chipCoroutine);
        chipCoroutine = StartCoroutine(SmoothChip(ratio));
    }

    private void UpdateVisual(RawImage img, float ratio)
    {
        if (img == null) return;
        
        // ถ้า maxWidth ยังไม่ได้ถูกตั้งค่า (เช่น Awake ยังไม่ทำงาน) ให้หาค่าตอนนี้เลย
        if (maxWidth <= 0)
            maxWidth = img.rectTransform.sizeDelta.x;

        // ตัดภาพจากขวาไปซ้ายโดยไม่บีบสัดส่วน
        img.rectTransform.sizeDelta = new Vector2(maxWidth * ratio, img.rectTransform.sizeDelta.y);
        img.uvRect = new Rect(0, 0, ratio, 1);
    }

    private IEnumerator SmoothChip(float targetRatio)
    {
        yield return new WaitForSeconds(0.5f); // หน่วงเวลาก่อนเลือดแดงจะลดตาม
        
        while (Mathf.Abs(currentChipRatio - targetRatio) > 0.001f)
        {
            currentChipRatio = Mathf.Lerp(currentChipRatio, targetRatio, Time.deltaTime * chipSpeed * 10f);
            UpdateVisual(chipRawImage, currentChipRatio);
            yield return null;
        }
        currentChipRatio = targetRatio;
        UpdateVisual(chipRawImage, currentChipRatio);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}
