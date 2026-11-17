using UnityEngine;

public class CliffEdgeZone : MonoBehaviour
{
    [Header("Anxiety Settings")]
    public float anxietyPerSecond = 8f;
    public bool showWarningThought = true;
    [TextArea]
    public string edgeThought = "Don't look down... One wrong step off this ridge and I'm done.";

    private bool hasWarned = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                // Increase anxiety while near the cliff edge
                stats.anxiety = Mathf.Clamp(stats.anxiety + anxietyPerSecond * Time.deltaTime, 0f, 100f);

                if (showWarningThought && !hasWarned)
                {
                    hasWarned = true;
                    // Calls your thought method if available on PlayerStats
                    stats.SendMessage("ShowThought", edgeThought, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Reset warning so it can trigger again later if they return
            hasWarned = false;
        }
    }
}