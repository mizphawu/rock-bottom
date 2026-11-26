using UnityEngine;

public class ForageItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Wild Berries";
    [TextArea] public string customThought = "Sweet berries... my breathing is steadier.";

    [Header("Survival Effects")]
    public float foodRestore = 20f;
    public float staminaRestore = 25f;
    public float anxietyRelief = 40f;

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
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                stats.EatFood(foodRestore, staminaRestore, anxietyRelief, itemName, customThought);
                Destroy(gameObject);
            }
        }
    }
}