using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Framing")]
    public Transform target; // Drag CameraTarget here
    public float distance = 4.5f;

    [Header("Smooth Orbit Settings")]
    public float mouseSensitivity = 0.08f;
    public float rotationSmoothTime = 0.08f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Collision")]
    public LayerMask collisionMask;

    private float targetYaw = 0f;
    private float targetPitch = 15f;
    private float currentYaw = 0f;
    private float currentPitch = 15f;
    private float yawVelocity;
    private float pitchVelocity;
    private PlayerStats playerStats;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (target != null)
        {
            targetYaw = currentYaw = target.eulerAngles.y;
            playerStats = target.GetComponentInParent<PlayerStats>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read New Input System Mouse Delta
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            targetYaw += mouseDelta.x * mouseSensitivity;
            targetPitch -= mouseDelta.y * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        // 2. Smoothly Glide Camera Angles
        currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        // 3. Calculate Desired Position & Rotation
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance);

        // 4. Prevent Camera from Clipping Through Mountain Terrain
        if (Physics.Linecast(target.position, desiredPosition, out RaycastHit hit, collisionMask))
        {
            desiredPosition = hit.point + hit.normal * 0.2f;
        }

        // 5. Add Subtle Anxiety Camera Shake When Panicking
        if (playerStats != null && playerStats.anxiety > 30f)
        {
            float shakeIntensity = (playerStats.anxiety / 100f) * 0.06f;
            desiredPosition += Random.insideUnitSphere * shakeIntensity;
        }

        transform.position = desiredPosition;
        transform.LookAt(target.position);
    }
}