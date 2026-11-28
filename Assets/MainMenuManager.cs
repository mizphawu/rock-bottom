using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene & Panels")]
    public string gameSceneName = "Modular Scene";
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject gameOverPanel;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;   // full-screen black panel
    public float fadeDuration = 1.5f; 
    public AudioSource musicAudioSource;  // optional, falls back to BGMPlayer

    private bool isLoading;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Check if we arrived here because Keri died in Modular Scene
        if (PlayerPrefs.GetInt("ShowGameOver", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowGameOver", 0); // Reset so normal launches show Main Menu
            PlayerPrefs.Save();

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
        else
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }
    }

    // Wire this to BOTH your Main Menu "Play" button and Game Over "Play Again" button
    public void PlayGame()
    {
        if (isLoading) return; // ignore double clicks
        StartCoroutine(FadeAndLoad());
    }

    // Wire this to your Game Over "Back to Main" button
    public void BackToMainMenu()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private IEnumerator FadeAndLoad()
    {
        isLoading = true;

        // Find the music source
        AudioSource music = musicAudioSource;

        float startVolume = music != null ? music.volume : 0f;

        // Show the fade panel and block further clicks
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.blocksRaycasts = true;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = k;
            if (music != null) music.volume = Mathf.Lerp(startVolume, 0f, k);

            yield return null;
        }

        // Stop the menu music and restore its volume for whatever plays next
        if (music != null)
        {
            music.Stop();
            music.volume = startVolume;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game triggered!");
        Application.Quit();
    }
}