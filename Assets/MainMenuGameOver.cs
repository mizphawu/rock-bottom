using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuGameOver : MonoBehaviour
{
    [Header("Drag Your Game Over Panel Here")]
    public GameObject gameOverPanel;

    [Header("Exact Gameplay Scene Name")]
    public string gameplaySceneName = "Modular Scene";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Check if we arrived here because Keri died in Modular Scene
        if (PlayerPrefs.GetInt("ShowGameOver", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowGameOver", 0); // Reset flag so normal launches show Main Menu
            PlayerPrefs.Save();

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
        else
        {
            // Normal startup -> hide Game Over panel
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }
    }

    // Wire this to your "Play Again" button
    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    // Wire this to your "Back to Main" button
    public void BackToMainMenu()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}