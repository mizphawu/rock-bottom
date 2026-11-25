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
    public float turnSpeed = 15f;
    public float gravity = -18f;
    public float jumpHeight = 1.2f;

    [Header("Terrain Slope Climbing")]
    public float climbSpeed = 3.2f;
    public float wallCheckDistance = 1.1f;
    public float minClimbSlopeAngle = 50f;
    public LayerMask climbableLayer;

    private CharacterController controller;
    private Vector3 velocity;
    private float highestAirY;
    private bool wasGrounded;
    private bool isJumping;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (stats == null)
            stats = GetComponent<PlayerStats>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // FIX 1: Unparent camera at runtime if it's inside the player object
        // to stop the player rotation from spinning the camera in a 360 loop.
        if (cameraTransform != null && cameraTransform.IsChildOf(transform))
        {
            Debug.LogWarning("Camera was parented to Player, causing a rotation loop! Detaching camera.");
            cameraTransform.SetParent(null);
        }

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

        Vector3 inputDir = new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        // 1. Check Steep Terrain Slope in Front
        int mask = climbableLayer.value == 0
            ? Physics.DefaultRaycastLayers
            : climbableLayer.value;

        bool steepSlopeInFront = false;
        Vector3 wallNormal = Vector3.zero;

        if (Physics.Raycast(
            transform.position + Vector3.up * 1f,
            transform.forward,
            out RaycastHit wallHit,
            wallCheckDistance,
            mask,
            QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(wallHit.normal, Vector3.up);

            if (slopeAngle >= minClimbSlopeAngle)
            {
                steepSlopeInFront = true;
                wallNormal = wallHit.normal;
            }
        }

        // Climb ONLY when holding E against a steep slope
        bool wantsToClimb =
            steepSlopeInFront &&
            kb.eKey.isPressed &&
            stats != null &&
            !stats.isExhausted &&
            stats.stamina > 1f;

        if (wantsToClimb)
        {
            stats.isClimbing = true;
            stats.isSprinting = false;
            stats.isMoving = true;

            isJumping = false;
            velocity.y = 0f;
            highestAirY = transform.position.y;

            Vector3 faceWallDir = new Vector3(-wallNormal.x, 0f, -wallNormal.z);
            if (faceWallDir.sqrMagnitude > 0.01f)
            {
                RotateCharacter(faceWallDir.normalized);
            }

            float verticalClimb = kb.sKey.isPressed ? -climbSpeed : climbSpeed;
            Vector3 climbMove = (Vector3.up * verticalClimb) + (transform.forward * 1.2f);

            controller.Move(climbMove * Time.deltaTime);

            UpdateAnimator(true, false, true, false);
            return;
        }
        else if (stats != null)
        {
            stats.isClimbing = false;
        }

        // 2. Camera-Relative Movement & Smooth Character Turning
        Vector3 moveDir = Vector3.zero;
        bool moving = inputDir.sqrMagnitude >= 0.01f;

        if (moving)
        {
            if (cameraTransform != null)
            {
                // FIX 2: Use flat Y-axis camera angle to prevent gimbal/pitch glitches
                float camYaw = cameraTransform.eulerAngles.y;
                Quaternion flatCamRot = Quaternion.Euler(0f, camYaw, 0f);
                moveDir = (flatCamRot * inputDir).normalized;
            }
            else
            {
                moveDir = inputDir;
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                RotateCharacter(moveDir);
            }
        }

        // 3. Ground, Jump & Fall Detection
        if (controller.isGrounded && velocity.y <= 0f)
        {
            if (!wasGrounded)
            {
                float fallDistance = highestAirY - transform.position.y;
                if (stats != null)
                    stats.OnLanded(fallDistance);

                if (animator != null)
                    animator.ResetTrigger("Jump");
            }

            isJumping = false;
            highestAirY = transform.position.y;
            velocity.y = -2f;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                isJumping = true;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    animator.ResetTrigger("Jump");
                    animator.SetTrigger("Jump");
                }
            }
        }
        else
        {
            if (transform.position.y > highestAirY)
                highestAirY = transform.position.y;
        }

        wasGrounded = controller.isGrounded;

        // 4. Moving & Sprinting State
        bool sprinting =
            moving &&
            kb.leftShiftKey.isPressed &&
            stats != null &&
            !stats.isExhausted;

        if (stats != null)
        {
            stats.isMoving = moving;
            stats.isSprinting = sprinting;
        }

        float currentSpeed = sprinting ? sprintSpeed : walkSpeed;

        velocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = (moveDir * currentSpeed) + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        bool isFalling = !controller.isGrounded && velocity.y < -0.5f;
        UpdateAnimator(moving, sprinting, false, isFalling);
    }

    // FIX 3: Strict Y-axis rotation helper so character never tilts or spins wildly
    void RotateCharacter(Vector3 targetDirection)
    {
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-turnSpeed * Time.deltaTime) // Frame-rate independent smooth damping
        );
    }

    void UpdateAnimator(bool moving, bool sprinting, bool climbing, bool falling)
    {
        if (animator == null) return;

        float animSpeed = !moving ? 0f : (sprinting ? 1f : 0.5f);

        // FIX 4: Set Speed directly without dampTime lag so walk animation starts immediately
        animator.SetFloat("Speed", animSpeed);
        animator.SetBool("IsClimbing", climbing);
        animator.SetBool("IsFalling", falling);
    }
}
