using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public PlayerStats stats;

    [Header("Movement & Sprint")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float gravity = -18f;
    public float jumpHeight = 1.2f;

    [Header("Wall Climbing")]
    public float climbSpeed = 3.2f;
    public float wallCheckDistance = 0.9f;
    public LayerMask climbableLayer;

    private CharacterController controller;
    private Vector3 velocity;
    private float highestAirY;
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        highestAirY = transform.position.y;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Read WASD / Arrow keys via New Input System
        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool cameraIsChild = cameraTransform != null && cameraTransform.IsChildOf(transform);

        Vector3 moveDir = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            if (cameraIsChild)
            {
                moveDir = (transform.right * h + transform.forward * v).normalized;
            }
            else
            {
                float camY = cameraTransform != null ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camY;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
                moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }
        }

        // 1. Wall Climbing (Hold E or push W against a climbable wall)
        int mask = climbableLayer.value == 0 ? Physics.DefaultRaycastLayers : climbableLayer.value;
        bool wallInFront = Physics.Raycast(transform.position + Vector3.up * 1f, transform.forward, wallCheckDistance, mask, QueryTriggerInteraction.Ignore);
        bool wantsToClimb = wallInFront && (kb.eKey.isPressed || v > 0.1f) && stats != null && !stats.isExhausted && stats.stamina > 1f;

        if (wantsToClimb)
        {
            stats.isClimbing = true;
            stats.isSprinting = false;
            stats.isMoving = true;

            velocity.y = 0f;
            highestAirY = transform.position.y;

            float verticalClimb = kb.sKey.isPressed ? -climbSpeed : climbSpeed;
            Vector3 climbMove = (Vector3.up * verticalClimb) + (transform.forward * 1.5f);
            controller.Move(climbMove * Time.deltaTime);
            return;
        }
        else if (stats != null)
        {
            stats.isClimbing = false;
        }

        // 2. Ground & Fall Detection
        if (controller.isGrounded)
        {
            if (!wasGrounded)
            {
                float fallDistance = highestAirY - transform.position.y;
                if (stats != null) stats.OnLanded(fallDistance);
            }
            highestAirY = transform.position.y;

            if (velocity.y < 0f) velocity.y = -2f;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            if (transform.position.y > highestAirY) highestAirY = transform.position.y;
        }
        wasGrounded = controller.isGrounded;

        // 3. Moving & Sprinting State (Stamina ONLY restores when staying still)
        bool moving = inputDir.magnitude >= 0.1f;
        bool sprinting = moving && kb.leftShiftKey.isPressed && stats != null && !stats.isExhausted;

        if (stats != null)
        {
            stats.isMoving = moving;
            stats.isSprinting = sprinting;
        }

        float currentSpeed = sprinting ? sprintSpeed : walkSpeed;
        controller.Move(moveDir * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}