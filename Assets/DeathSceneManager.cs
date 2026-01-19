using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class DeathSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restartButton;
    public Button exitButton;
    public GameObject choicePanel;

    [Header("Video Settings")]
    public VideoPlayer deathVideoPlayer;
    public RawImage videoDisplay;
    public AudioSource videoAudio;

    [Header("Settings")]
    public float videoFadeInTime = 1f;
    public float videoFadeOutTime = 1f;

    private bool videoFinished = false;

    void Start()
    {
        // Hide choice panel initially
        if (choicePanel != null)
            choicePanel.SetActive(false);

        // Set up button listeners
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        // Set up video player
        if (deathVideoPlayer != null)
        {
            deathVideoPlayer.loopPointReached += OnVideoFinished;
            deathVideoPlayer.Play();

            // Fade in video
            StartCoroutine(FadeVideo(0f, 1f, videoFadeInTime));
        }
        else
        {
            // If no video, show choices immediately
            ShowChoicePanel();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;

        // Fade out video
        StartCoroutine(FadeVideo(1f, 0f, videoFadeOutTime, () => {
            if (videoDisplay != null)
                videoDisplay.gameObject.SetActive(false);

            ShowChoicePanel();
        }));
    }

    System.Collections.IEnumerator FadeVideo(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        if (videoDisplay == null) yield break;

        Color color = videoDisplay.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            videoDisplay.color = color;
            yield return null;
        }

        color.a = endAlpha;
        videoDisplay.color = color;

        onComplete?.Invoke();
    }

    void ShowChoicePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);

            // Optional: Fade in panel
            CanvasGroup canvasGroup = choicePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = choicePanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f));
        }
    }

    System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        group.alpha = end;
    }

    void RestartGame()
    {
        // Get the last played scene from PlayerPrefs
        string lastScene = PlayerPrefs.GetString("LastScene", "Level1");

        Debug.Log("Restarting game: " + lastScene);

        // Load the last scene
        SceneManager.LoadScene(lastScene);
    }

    void ExitGame()
    {
        Debug.Log("Exiting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        // Skip video with any key/mouse click
        if (!videoFinished && deathVideoPlayer != null && deathVideoPlayer.isPlaying)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                deathVideoPlayer.Stop();
                OnVideoFinished(deathVideoPlayer);
            }
        }
    }
}