using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float focusHeightOffset = 1.4f;

    [Header("Camera Settings")]
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 7f;
    public float zoomSpeed = 0.05f;

    [Header("Mouse Look")]
    public float baseMouseSensitivity = 0.08f; // Base speed multiplied by Settings slider (default 1.4x)
    public float minPitch = -35f;
    public float maxPitch = 65f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;

    private float yaw;
    private float pitch = 15f;
    private PlayerStats playerStats;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            yaw = target.eulerAngles.y;
            playerStats = target.GetComponentInChildren<PlayerStats>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Re-lock cursor when clicking inside the game screen if it got unlocked by UI
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 1. Mouse camera rotation & scroll zoom (connected to Settings MouseSensitivity)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            // Reads the exact slider multiplier saved by SettingsMenuManager (default 1.4x)
            float savedMultiplier = PlayerPrefs.GetFloat("MouseSensitivity", 1.4f);
            float effectiveSensitivity = baseMouseSensitivity * savedMultiplier;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * effectiveSensitivity;
            pitch -= mouseDelta.y * effectiveSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            float scroll = Mouse.current.scroll.ReadValue().y;
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // 2. Calculate camera rotation & focus point
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + Vector3.up * focusHeightOffset;

        // 3. Position camera behind player
        Vector3 desiredPosition = focusPoint - (cameraRotation * Vector3.forward * distance);

        // 4. Camera wall/terrain collision
        Vector3 direction = (desiredPosition - focusPoint).normalized;

        if (Physics.SphereCast(
            focusPoint,
            0.2f,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                desiredPosition = focusPoint + direction * Mathf.Max(0.2f, hit.distance - 0.2f);
            }
        }

        // 5. Anxiety camera shake
        if (playerStats != null && playerStats.anxiety > 30f)
        {
            float shake = (playerStats.anxiety / 100f) * 0.03f;
            desiredPosition += Random.insideUnitSphere * shake;
        }

        transform.position = desiredPosition;
        transform.rotation = cameraRotation;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}