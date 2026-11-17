using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("UI Slot (Drag ThoughtText Here)")]
    public TextMeshProUGUI thoughtText;

    [Header("Fear of Heights Slot")]
    public float cliffHeightThreshold = 6f;
    public LayerMask groundLayer;

    [Header("Meters (0 to 100)")]
    public float stamina = 100f;
    public float hunger = 100f; // 100 = full, 0 = starving
    public float anxiety = 0f;  // 0 = calm, 100 = panic

    [Header("Drain & Regen Rates")]
    public float sprintStaminaDrain = 20f;
    public float staminaRegen = 12f;
    public float passiveHungerDrain = 1.5f;
    public float panicAnxietyIncrease = 15f;
    public float calmAnxietyDecay = 5f;

    [HideInInspector] public bool isSprinting;
    [HideInInspector] public bool isExhausted;
    [HideInInspector] public bool isBeingChased;

    private bool warnedHunger = false;
    private bool warnedHeights = false;

    void Update()
    {
        HandleHunger();
        HandleStamina();
        HandleAnxiety();
    }

    void HandleHunger()
    {
        hunger = Mathf.Clamp(hunger - passiveHungerDrain * Time.deltaTime, 0f, 100f);

        if (hunger < 35f && !warnedHunger)
        {
            warnedHunger = true;
            ShowThought("My stomach is cramping... I need berries or mushrooms.");
        }
        else if (hunger >= 50f)
        {
            warnedHunger = false;
        }
    }

    void HandleStamina()
    {
        float currentRegen = hunger < 25f ? staminaRegen * 0.4f : staminaRegen;

        if (isSprinting && !isExhausted)
        {
            stamina -= sprintStaminaDrain * Time.deltaTime;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isExhausted = true;
                ShowThought("I'm out of breath... I can't run.");
            }
        }
        else
        {
            stamina += currentRegen * Time.deltaTime;

            if (isExhausted && stamina >= 30f)
            {
                isExhausted = false;
            }
        }

        stamina = Mathf.Clamp(stamina, 0f, 100f);
    }

    void HandleAnxiety()
    {
        bool nearDrop = CheckFearOfHeights();

        if (nearDrop && !warnedHeights)
        {
            warnedHeights = true;
            ShowThought("It's way too high... my hands are shaking.");
        }
        else if (!nearDrop)
        {
            warnedHeights = false;
        }

        if (isBeingChased || nearDrop)
            anxiety += panicAnxietyIncrease * Time.deltaTime;
        else
            anxiety -= calmAnxietyDecay * Time.deltaTime;

        anxiety = Mathf.Clamp(anxiety, 0f, 100f);
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

    public void EatFood(float foodAmount, float staminaAmount, float anxietyRelief, string foodName, string customThought = "")
    {
        hunger = Mathf.Clamp(hunger + foodAmount, 0f, 100f);
        stamina = Mathf.Clamp(stamina + staminaAmount, 0f, 100f);
        anxiety = Mathf.Clamp(anxiety - anxietyRelief, 0f, 100f);

        if (stamina >= 30f) isExhausted = false;

        string message = string.IsNullOrEmpty(customThought)
            ? $"Ate {foodName}. Feeling steadier."
            : customThought;

        ShowThought(message);
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