using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7.5f;
    public float turnSmoothTime = 0.08f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform; // Drag Main Camera here

    private CharacterController controller;
    private PlayerStats stats;
    private Vector3 velocity;
    private float turnSmoothVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        // 1. Check Ground & Apply Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        // 2. Always Rotate Character to Face Camera Direction (Even When Standing Still)
        if (cameraTransform != null)
        {
            float cameraYaw = cameraTransform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraYaw, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        // 3. Read New Input System Keyboard (WASD / Arrow Keys)
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
        }

        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        // 4. Move Relative to Where She Is Facing (Forward, Back, Strafe Left/Right)
        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 moveDir = transform.right * inputDir.x + transform.forward * inputDir.z;

            // Check Sprint (Left Shift) & Exhaustion — Only Sprint Forward
            bool wantsToSprint = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && z > 0f;
            bool canSprint = stats == null || (!stats.isExhausted && stats.stamina > 1f);

            float currentSpeed = walkSpeed;
            if (wantsToSprint && canSprint)
            {
                currentSpeed = sprintSpeed;
                if (stats != null) stats.isSprinting = true;
            }
            else
            {
                if (stats != null) stats.isSprinting = false;
            }

            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            if (stats != null) stats.isSprinting = false;
        }

        // 5. Apply Gravity
        controller.Move(velocity * Time.deltaTime);
    }
}