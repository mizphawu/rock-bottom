using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using MagicPigGames; // Connects to your Horizontal & Vertical ProgressBar scripts

public class PlayerStats : MonoBehaviour
{
    [Header("UI Bars & Text (Drag ProgressBars Here)")]
    public ProgressBar staminaBar;
    public ProgressBar anxietyBar;
    public TextMeshProUGUI thoughtText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public string mainMenuSceneName = "MainMenu"; // Set this to your exact Main Menu scene name

    [Header("Meters (0 to 100)")]
    public float stamina = 100f;
    public float anxiety = 0f; // 0 = calm, 100 = panic

    [Header("Stamina Rates")]
    public float sprintStaminaDrain = 18f;
    public float climbStaminaDrain = 22f;
    public float stillStaminaRegen = 20f; // Restores ONLY when staying still

    [Header("Anxiety & High Places")]
    public float cliffHeightThreshold = 6f;
    public float highClimbHeight = 4f;
    public LayerMask groundLayer;
    public float panicAnxietyIncrease = 15f;
    public float calmAnxietyDecay = 4f;
    public float highAnxietyThreshold = 70f; // High anxiety penalizes stamina

    [Header("Panic Death Timers")]
    public float ledgePeakTimeLimit = 7f;    // 7s at peak anxiety on wall/ledge -> Game Over
    public float monsterCatchTimeLimit = 5f; // 5s after monster catch to eat berry -> Game Over

    [HideInInspector] public bool isMoving;
    [HideInInspector] public bool isSprinting;
    [HideInInspector] public bool isClimbing;
    [HideInInspector] public bool isExhausted;
    [HideInInspector] public bool isBeingChased;

    private bool warnedHeights = false;
    private bool warnedHighAnxiety = false;
    private bool isCaught = false;
    private bool isDead = false;

    private float ledgePeakTimer = 0f;
    private float monsterCatchTimer = 0f;

    void Start()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateBars();
    }

    void Update()
    {
        if (isDead) return;

        HandleStamina();
        HandleAnxiety();
        HandlePanicDeathTimers();
        UpdateBars();
    }

    void HandleStamina()
    {
        bool draining = (isSprinting || isClimbing) && !isExhausted;

        if (draining)
        {
            float drainRate = isClimbing ? climbStaminaDrain : sprintStaminaDrain;
            // High anxiety makes stamina drain 35% faster
            if (anxiety >= highAnxietyThreshold) drainRate *= 1.35f;

            stamina -= drainRate * Time.deltaTime;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isExhausted = true;
                isClimbing = false;
                ShowThought("I'm out of breath... I need to stop and stand still.");
            }
        }
        else if (!isMoving && !isClimbing)
        {
            // Restores ONLY when staying still
            float regen = anxiety >= highAnxietyThreshold ? stillStaminaRegen * 0.5f : stillStaminaRegen;
            stamina += regen * Time.deltaTime;

            if (isExhausted && stamina >= 30f)
            {
                isExhausted = false;
            }
        }

        stamina = Mathf.Clamp(stamina, 0f, 100f);
    }

    void HandleAnxiety()
    {
        // While caught by monster, anxiety stays locked at peak (100) until she eats a berry!
        if (isCaught)
        {
            anxiety = 100f;
            return;
        }

        bool nearDrop = CheckFearOfHeights();
        bool climbingHigh = isClimbing && CheckHeightAboveGround() >= highClimbHeight;

        if ((nearDrop || climbingHigh) && !warnedHeights)
        {
            warnedHeights = true;
            ShowThought("It's way too high... my hands are shaking.");
        }
        else if (!nearDrop && !climbingHigh)
        {
            warnedHeights = false;
        }

        if (isBeingChased || nearDrop || climbingHigh)
        {
            anxiety += panicAnxietyIncrease * Time.deltaTime;
        }
        else
        {
            anxiety -= calmAnxietyDecay * Time.deltaTime;
        }

        anxiety = Mathf.Clamp(anxiety, 0f, 100f);

        if (anxiety >= highAnxietyThreshold && !warnedHighAnxiety)
        {
            warnedHighAnxiety = true;
            ShowThought("My heart is pounding... I'm losing stamina fast! I need fruit or mushrooms.");
        }
        else if (anxiety < highAnxietyThreshold - 10f)
        {
            warnedHighAnxiety = false;
        }
    }

    void HandlePanicDeathTimers()
    {
        bool atPeak = anxiety >= 100f;

        // 1. Caught by Monster: 5 seconds to eat a berry or Game Over
        if (isCaught && atPeak)
        {
            monsterCatchTimer += Time.deltaTime;
            if (monsterCatchTimer >= monsterCatchTimeLimit)
            {
                TriggerGameOver();
                return;
            }
        }
        else
        {
            monsterCatchTimer = 0f;
        }

        // 2. Hanging on wall (isClimbing) or standing on ledge (CheckFearOfHeights) at peak anxiety > 7s -> Game Over
        bool onWallOrLedge = isClimbing || CheckFearOfHeights();
        if (atPeak && onWallOrLedge)
        {
            ledgePeakTimer += Time.deltaTime;
            if (ledgePeakTimer >= ledgePeakTimeLimit)
            {
                TriggerGameOver();
                return;
            }
        }
        else
        {
            ledgePeakTimer = 0f;
        }
    }

    bool CheckFearOfHeights()
    {
        Vector3 checkOrigin = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        int mask = groundLayer.value == 0 ? Physics.DefaultRaycastLayers : groundLayer.value;

        if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit hit, 50f, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.distance > cliffHeightThreshold;
        }
        return true;
    }

    float CheckHeightAboveGround()
    {
        int mask = groundLayer.value == 0 ? Physics.DefaultRaycastLayers : groundLayer.value;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 50f, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.distance;
        }
        return 50f;
    }

    public void OnLanded(float fallDistance)
    {
        if (fallDistance >= 3.0f)
        {
            float anxietySpike = Mathf.Clamp(fallDistance * 6f, 12f, 55f);
            anxiety = Mathf.Clamp(anxiety + anxietySpike, 0f, 100f);
            UpdateBars();
            ShowThought("That drop almost made my heart stop...");
        }
    }

    public void OnCaughtByMonster()
    {
        if (isCaught || isDead) return;

        isCaught = true;
        monsterCatchTimer = 0f;
        anxiety = 100f;
        UpdateBars();
        ShowThought("It got me! I need to eat a berry right now!", 4.5f);
    }

    public void TriggerGameOver()
    {
        if (isDead) return;
        isDead = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    // Hook this to your Game Over "Play Again" button
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Hook this to your Game Over "Main Menu" button
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void EatFruit(float staminaAmount, float anxietyRelief, string fruitName, string customThought = "")
    {
        stamina = Mathf.Clamp(stamina + staminaAmount, 0f, 100f);
        anxiety = Mathf.Clamp(anxiety - anxietyRelief, 0f, 100f);

        // If she ate a berry/fruit and lowered anxiety below 100, cancel the monster catch panic!
        if (anxiety < 100f)
        {
            isCaught = false;
            monsterCatchTimer = 0f;
        }

        if (stamina >= 30f) isExhausted = false;

        string message = string.IsNullOrEmpty(customThought)
            ? $"Ate {fruitName}. My breathing is steadier."
            : customThought;

        ShowThought(message);
        UpdateBars();
    }

    public void EatFood(float unusedHunger, float staminaAmount, float anxietyRelief, string foodName, string customThought = "")
    {
        EatFruit(staminaAmount, anxietyRelief, foodName, customThought);
    }

    void UpdateBars()
    {
        // MagicPigGames ProgressBar uses SetProgress(0f to 1f)
        if (staminaBar != null) staminaBar.SetProgress(stamina / 100f);
        if (anxietyBar != null) anxietyBar.SetProgress(anxiety / 100f);
    }

    public void ShowThought(string message, float duration = 3f)
    {
        if (thoughtText == null) return;
        StopAllCoroutines();
        StartCoroutine(DisplayThoughtRoutine(message, duration));
    }

    IEnumerator DisplayThoughtRoutine(string message, float duration)
    {
        thoughtText.text = $"\"{message}\"";
        yield return new WaitForSeconds(duration);
        thoughtText.text = "";
    }
}