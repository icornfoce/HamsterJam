using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;
using System;

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Video Clips")]
    [SerializeField] private VideoClip introClip;
    [SerializeField] private VideoClip playerDeathClip;
    [SerializeField] private VideoClip bossDeathClip;
    [SerializeField] private VideoClip logoutClip;

    private Action onVideoComplete;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Ensure it's a root object for DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            
            // Initialize display if not already set
            SetupVideoDisplay();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.loopPointReached += OnMovieFinished;
        
        if (videoPanel != null)
            videoPanel.SetActive(false);
    }

    private void SetupVideoDisplay()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoDisplay == null) return;

        // If the user hasn't assigned a texture to the RawImage or it's not a RenderTexture
        // we can create one dynamically to ensure it works.
        if (videoDisplay.texture == null || !(videoDisplay.texture is RenderTexture))
        {
            RenderTexture rt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            videoDisplay.texture = rt;
            videoPlayer.targetTexture = rt;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }
    }

    private void Start()
    {
        // Play Intro automatically and pause the game until it finishes
        PlayIntro();
    }

    public void PlayIntro(Action callback = null)
    {
        PlayVideo(introClip, callback);
    }

    public void PlayPlayerDeath(Action callback = null)
    {
        PlayVideo(playerDeathClip, () => {
            callback?.Invoke();
            RestartScene();
        });
    }

    public void PlayBossDeath(Action callback = null)
    {
        PlayVideo(bossDeathClip, () => {
            callback?.Invoke();
            RestartScene();
        });
    }

    private void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void PlayLogout(Action callback = null)
    {
        PlayVideo(logoutClip, callback);
    }

    private void PlayVideo(VideoClip clip, Action callback)
    {
        if (clip == null)
        {
            Debug.LogWarning("Video clip is null!");
            callback?.Invoke();
            return;
        }

        // Pause the game while video is playing
        Time.timeScale = 0f;

        // Stop any current playback to clear buffers
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        onVideoComplete = callback;
        
        if (videoPanel != null)
            videoPanel.SetActive(true);

        // Optional: Ensure Audio Output is handled cleanly
        // Setting to Direct can sometimes cause buffer overflows on certain hardware
        // videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource; 

        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        
        // Prepare first to ensure buffers are ready
        videoPlayer.Prepare();
        StartCoroutine(WaitForPrepareAndPlay());
    }

    private IEnumerator WaitForPrepareAndPlay()
    {
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        videoPlayer.Play();
    }

    private void OnMovieFinished(VideoPlayer vp)
    {
        // Resume the game
        Time.timeScale = 1f;

        if (videoPanel != null)
        {
            // ปิด UI วิดีโอ
            videoPanel.SetActive(false);
            
            // ถ้ามี CanvasGroup ให้สั่งเลิกบล็อก Raycast ด้วยเพื่อความชัวร์
            CanvasGroup cg = videoPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        // Lock cursor back if needed (standard gameplay state)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onVideoComplete?.Invoke();
        onVideoComplete = null;
        
        Debug.Log("Video Finished and UI disabled.");
    }

    // Optional: Skip video
    private void Update()
    {
        if (videoPlayer.isPlaying && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            videoPlayer.Stop();
            OnMovieFinished(videoPlayer);
        }
    }
}
