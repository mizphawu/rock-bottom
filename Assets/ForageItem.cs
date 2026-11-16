using UnityEngine;

public class ForageItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Wild Berries";
    [TextArea] public string customThought = "Sweet berries... that gave me a quick burst of energy.";

    [Header("Survival Effects")]
    public float foodRestore = 20f;
    public float staminaRestore = 25f;
    public float anxietyRelief = 5f;

    [Header("Visual Polish")]
    public float rotateSpeed = 50f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.EatFood(foodRestore, staminaRestore, anxietyRelief, itemName, customThought);
                Destroy(gameObject);
            }
        }
    }
}