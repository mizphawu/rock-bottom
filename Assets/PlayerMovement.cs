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
    public float wallCheckDistance = 1.2f;
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

        // 1. Detect Cliff Wall (In Front OR Below Ledge when pressing E)
        int mask = climbableLayer.value == 0
            ? Physics.DefaultRaycastLayers
            : climbableLayer.value;

        bool foundClimbableWall = false;
        Vector3 wallNormal = Vector3.zero;
        bool grabbedTopLedge = false;

        Vector3 chestOrigin = transform.position + Vector3.up * 1.0f;
        Vector3 kneeOrigin = transform.position + Vector3.up * 0.35f;

        // Check A: Mountain/cliff wall directly in front (chest or knee level)
        if (Physics.Raycast(chestOrigin, transform.forward, out RaycastHit wallHit, wallCheckDistance, mask, QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(kneeOrigin, transform.forward, out wallHit, wallCheckDistance, mask, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(wallHit.normal, Vector3.up);
            if (slopeAngle >= minClimbSlopeAngle)
            {
                foundClimbableWall = true;
                wallNormal = wallHit.normal;
            }
        }
        // Check B: Standing at top edge facing outward -> press E to turn around and grab the cliff below
        else if (kb.eKey.isPressed)
        {
            // Probe slightly ahead and below the player's feet, then raycast back toward the cliff face
            Vector3[] probeOffsets = new Vector3[]
            {
                (transform.forward * 0.7f) + (Vector3.down * 0.55f),
                (transform.forward * 1.0f) + (Vector3.down * 0.9f)
            };

            foreach (Vector3 offset in probeOffsets)
            {
                Vector3 probeOrigin = transform.position + offset;
                if (Physics.Raycast(probeOrigin, -transform.forward, out RaycastHit ledgeHit, 1.8f, mask, QueryTriggerInteraction.Ignore))
                {
                    float slopeAngle = Vector3.Angle(ledgeHit.normal, Vector3.up);
                    if (slopeAngle >= minClimbSlopeAngle)
                    {
                        foundClimbableWall = true;
                        wallNormal = ledgeHit.normal;
                        grabbedTopLedge = true;
                        break;
                    }
                }
            }
        }

        // Climb whenever holding E near a cliff wall or top ledge
        bool wantsToClimb =
            foundClimbableWall &&
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

            // Direction straight INTO the mountain wall (opposite of the cliff's outward normal)
            Vector3 faceMountainDir = new Vector3(-wallNormal.x, 0f, -wallNormal.z).normalized;

            if (faceMountainDir.sqrMagnitude > 0.01f)
            {
                // Snap immediately to face the mountain wall on ledge grab so player never faces outward
                if (grabbedTopLedge)
                    transform.rotation = Quaternion.LookRotation(faceMountainDir);
                else
                    RotateCharacter(faceMountainDir);
            }

            // If grabbing from the top ledge, step off the lip onto the cliff face while facing the mountain
            if (grabbedTopLedge)
            {
                Vector3 dropOntoWall = (-faceMountainDir * 0.35f) + (Vector3.down * 0.5f);
                controller.Move(dropOntoWall);
            }

            bool climbingDown = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            bool climbingUp = kb.wKey.isPressed || kb.upArrowKey.isPressed;

            // Default when holding E at a top ledge: start descending slightly or hold onto wall
            float verticalClimb = 0f;
            Vector3 wallStickMove = faceMountainDir * 0.6f; // Always hug INTO the mountain wall

            if (climbingDown)
            {
                verticalClimb = -climbSpeed;
                wallStickMove = faceMountainDir * 0.45f;

                // Stop climbing automatically when reaching flat ground at the bottom
                if (!grabbedTopLedge && controller.isGrounded)
                {
                    stats.isClimbing = false;
                }
            }
            else if (climbingUp)
            {
                verticalClimb = climbSpeed;
                wallStickMove = faceMountainDir * 1.1f;
            }

            Vector3 climbMove = (Vector3.up * verticalClimb) + wallStickMove;
            controller.Move(climbMove * Time.deltaTime);

            UpdateAnimator(climbingUp || climbingDown, false, true, false);
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

    void RotateCharacter(Vector3 targetDirection)
    {
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-turnSpeed * Time.deltaTime)
        );
    }

    void UpdateAnimator(bool moving, bool sprinting, bool climbing, bool falling)
    {
        if (animator == null) return;

        float animSpeed = !moving ? 0f : (sprinting ? 1f : 0.5f);

        animator.SetFloat("Speed", animSpeed);
        animator.SetBool("IsClimbing", climbing);
        animator.SetBool("IsFalling", falling);
    }
}