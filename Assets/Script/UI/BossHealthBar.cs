using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider mainSlider;
    [SerializeField] private Slider chipSlider; // For the "trailing" effect
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private string bossName = "FURNACE";
    [SerializeField] private float chipSpeed = 0.5f;
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine chipCoroutine;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (bossNameText != null) bossNameText.text = bossName;
    }

    public void Show()
    {
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
        float ratio = current / max;
        mainSlider.value = ratio;

        if (chipCoroutine != null) StopCoroutine(chipCoroutine);
        chipCoroutine = StartCoroutine(SmoothChip(ratio));
    }

    private IEnumerator SmoothChip(float targetRatio)
    {
        yield return new WaitForSeconds(0.5f); // Delay before chip follows
        
        while (Mathf.Abs(chipSlider.value - targetRatio) > 0.001f)
        {
            chipSlider.value = Mathf.Lerp(chipSlider.value, targetRatio, Time.deltaTime * chipSpeed * 10f);
            yield return null;
        }
        chipSlider.value = targetRatio;
    }

    private IEnumerator Fade(float targetAlpha)
    {
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
