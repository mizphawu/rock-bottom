
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7.5f;
    public float gravity = -9.81f;

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private PlayerStats stats;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
        }

        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        bool isMoving = inputDir.sqrMagnitude > 0.01f;

        bool wantsToSprint = Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed && z > 0f;

        bool canSprint = stats == null ||
            (!stats.isExhausted && stats.stamina > 1f);

        bool isSprinting = isMoving && wantsToSprint && canSprint;

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (stats != null)
            stats.isSprinting = isSprinting;

        // Movement relative to player facing
        if (isMoving)
        {
            Vector3 moveDir =
                transform.right * inputDir.x +
                transform.forward * inputDir.z;

            controller.Move(
                moveDir.normalized * currentSpeed * Time.deltaTime
            );
        }

        // Update Animator Blend Tree
        if (animator != null)
        {
            float animationSpeed = 0f;

            if (isMoving)
                animationSpeed = isSprinting ? 1f : 0.5f;

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        // Apply gravity
        controller.Move(velocity * Time.deltaTime);
    }
}
