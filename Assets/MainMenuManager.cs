using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene & Panels")]
    public string gameSceneName = "SampleScene";
    public GameObject settingsPanel;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;   // full-screen black panel
    public float fadeDuration = 1.5f; 
    public AudioSource musicAudioSource;  // optional, falls back to BGMPlayer

    private bool isLoading;

    public void PlayGame()
    {
        if (isLoading) return; // ignore double clicks
        StartCoroutine(FadeAndLoad());
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