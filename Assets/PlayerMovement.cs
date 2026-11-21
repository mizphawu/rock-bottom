using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public PlayerStats stats;
    public Animator animator;

    [Header("Movement & Sprint")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float turnSpeed = 12f;
    public float gravity = -18f;
    public float jumpHeight = 1.2f;

    [Header("Terrain Slope Climbing")]
    public float climbSpeed = 3.2f;
    public float wallCheckDistance = 1.1f;
    public float minClimbSlopeAngle = 50f; // Slopes >= 50° are climbable cliffs
    public LayerMask climbableLayer; // Leave as Nothing/Default to detect Terrain

    private CharacterController controller;
    private Vector3 velocity;
    private float highestAirY;
    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        highestAirY = transform.position.y;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        // 1. Check Steep Terrain Slope in Front
        int mask = climbableLayer.value == 0 ? Physics.DefaultRaycastLayers : climbableLayer.value;
        bool steepSlopeInFront = false;
        Vector3 wallNormal = Vector3.zero;

        if (Physics.Raycast(transform.position + Vector3.up * 1f, transform.forward, out RaycastHit wallHit, wallCheckDistance, mask, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(wallHit.normal, Vector3.up);
            if (slopeAngle >= minClimbSlopeAngle)
            {
                steepSlopeInFront = true;
                wallNormal = wallHit.normal;
            }
        }

        // Climb ONLY when holding E against a steep slope (>= 50°)
        bool wantsToClimb = steepSlopeInFront && kb.eKey.isPressed && stats != null && !stats.isExhausted && stats.stamina > 1f;

        if (wantsToClimb)
        {
            stats.isClimbing = true;
            stats.isSprinting = false;
            stats.isMoving = true;

            velocity.y = 0f;
            highestAirY = transform.position.y;

            // Lock rotation squarely toward the cliff face so it NEVER spins around
            Vector3 faceWallDir = new Vector3(-wallNormal.x, 0f, -wallNormal.z).normalized;
            if (faceWallDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceWallDir), turnSpeed * Time.deltaTime);
            }

            float verticalClimb = kb.sKey.isPressed ? -climbSpeed : climbSpeed;
            Vector3 climbMove = (Vector3.up * verticalClimb) + (transform.forward * 1.2f);
            controller.Move(climbMove * Time.deltaTime);

            UpdateAnimator(true, false, true);
            return;
        }
        else if (stats != null)
        {
            stats.isClimbing = false;
        }

        // 2. Normal Camera-Relative Turning & Movement
        Vector3 moveDir = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            float camY = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camY;
            Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            moveDir = targetRot * Vector3.forward;
        }

        // 3. Ground & Fall Detection
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

        // 4. Moving & Sprinting State (Stamina ONLY restores when staying still)
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

        UpdateAnimator(moving, sprinting, false);
    }

    void UpdateAnimator(bool moving, bool sprinting, bool climbing)
    {
        if (animator == null) return;
        float animSpeed = !moving ? 0f : (sprinting ? 1f : 0.5f);
        animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsClimbing", climbing);
    }
}