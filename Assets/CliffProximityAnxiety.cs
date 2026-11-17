using UnityEngine;

public class CliffProximityAnxiety : MonoBehaviour
{
    [Header("Cliff Detection")]
    public float lookAheadDistance = 2.0f;   // How far in front of the player to check
    public float dangerousDropDepth = 6.0f;  // How deep the drop must be to count as a cliff
    public float anxietyGainRate = 10f;      // Anxiety gained per second near edge
    public float calmDownRate = 2f;          // Optional slow recovery when safe

    private PlayerStats stats;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (stats == null) return;

        // Start ray 2 meters ahead of the player at waist height
        Vector3 checkOrigin = transform.position + Vector3.up * 1.0f + transform.forward * lookAheadDistance;

        bool nearCliff = false;

        if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit hit, 50f))
        {
            // If the ground 2m ahead is much lower than the player's feet, they're at a cliff edge
            float dropHeight = transform.position.y - hit.point.y;
            if (dropHeight >= dangerousDropDepth)
            {
                nearCliff = true;
            }
        }
        else
        {
            // Nothing below within 50m = sheer drop-off
            nearCliff = true;
        }

        if (nearCliff)
        {
            stats.anxiety = Mathf.Clamp(stats.anxiety + anxietyGainRate * Time.deltaTime, 0f, 100f);
        }
        else if (calmDownRate > 0f)
        {
            stats.anxiety = Mathf.Clamp(stats.anxiety - calmDownRate * Time.deltaTime, 0f, 100f);
        }
    }
}