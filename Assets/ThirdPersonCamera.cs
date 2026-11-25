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
    public float mouseSensitivity = 0.15f;
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

        if (target != null)
        {
            yaw = target.eulerAngles.y;
            playerStats = target.GetComponentInChildren<PlayerStats>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Mouse camera rotation & scroll zoom
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
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
