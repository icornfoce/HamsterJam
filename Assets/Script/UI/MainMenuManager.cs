using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene to Load")]
    public string gameSceneName = "GameScene";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;

    [Header("Transitions")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    [Header("Button Effects")]
    public float hoverScaleSize = 1.1f;
    public float animationSpeed = 10f;
    public Color hoverFadeColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public AudioSource sfxSource;
    public AudioClip hoverSFX;
    public AudioClip clickSFX;

    void Start()
    {
        // Ensure game is running at normal speed in menu
        Time.timeScale = 1f;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }

        SetupButtonEffects();
    }

    public void OnStartGame()
    {
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void SetupButtonEffects()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            if (btn.gameObject.GetComponent<ButtonEffectHandler>() != null) continue;
            ButtonEffectHandler handler = btn.gameObject.AddComponent<ButtonEffectHandler>();
            handler.Initialize(this, btn);
        }
    }

    private class ButtonEffectHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MainMenuManager manager;
        private Button button;
        private Graphic buttonGraphic;
        private Vector3 originalScale;
        private Color originalColor;
        private Vector3 targetScale;
        private Color targetColor;

        public void Initialize(MainMenuManager mgr, Button btn)
        {
            manager = mgr;
            button = btn;
            buttonGraphic = button.targetGraphic;

            // Disable built-in transition to use our custom lerp
            button.transition = Selectable.Transition.None;

            originalScale = transform.localScale;
            targetScale = originalScale;

            if (buttonGraphic != null)
            {
                originalColor = buttonGraphic.color;
                targetColor = originalColor;
            }

            button.onClick.AddListener(PlayClickSound);
        }

        void Update()
        {
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * manager.animationSpeed);
            }

            if (buttonGraphic != null && buttonGraphic.color != targetColor)
            {
                buttonGraphic.color = Color.Lerp(buttonGraphic.color, targetColor, Time.deltaTime * manager.animationSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            targetScale = originalScale * manager.hoverScaleSize;
            if (buttonGraphic != null) targetColor = originalColor * manager.hoverFadeColor;

            if (manager.hoverSFX != null && manager.sfxSource != null)
                manager.sfxSource.PlayOneShot(manager.hoverSFX);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
            if (buttonGraphic != null) targetColor = originalColor;
        }

        private void PlayClickSound()
        {
            if (manager.clickSFX != null && manager.sfxSource != null)
                manager.sfxSource.PlayOneShot(manager.clickSFX);
        }
    }
}