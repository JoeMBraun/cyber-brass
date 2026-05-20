using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberBrass.Player
{
    /// <summary>
    /// Implements a responsive, physics-based First Person Shooter player controller.
    /// Handles movement via CharacterController, camera orientation, jumping, and input events using the Unity Input System.
    /// Supports running, jumping, looking around, and gravity.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Standard walking speed in units per second.")]
        [SerializeField] private float walkSpeed = 6.0f;
        [Tooltip("Gravity acceleration multiplier.")]
        [SerializeField] private float gravity = -9.81f;
        [Tooltip("The height of the player's jump in units.")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Camera & Look Settings")]
        [Tooltip("The camera transform associated with the player's eyes.")]
        [SerializeField] private Transform playerCamera;
        [Tooltip("Sensitivity of mouse looking.")]
        [SerializeField] private float mouseSensitivity = 0.1f;
        [Tooltip("Minimum vertical angle the player can look (in degrees).")]
        [SerializeField] private float minPitch = -85f;
        [Tooltip("Maximum vertical angle the player can look (in degrees).")]
        [SerializeField] private float maxPitch = 85f;

        [Header("Input Bindings")]
        [SerializeField] private InputAction moveAction;
        [SerializeField] private InputAction lookAction;
        [SerializeField] private InputAction jumpAction;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _cameraPitch = 0.0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Setup default input bindings inline so they are configured by default
            if (moveAction == null || moveAction.bindings.Count == 0)
            {
                moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
                moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }

            if (lookAction == null || lookAction.bindings.Count == 0)
            {
                lookAction = new InputAction("Look", binding: "<Gamepad>/rightStick");
                lookAction.AddBinding("<Mouse>/delta");
            }

            if (jumpAction == null || jumpAction.bindings.Count == 0)
            {
                jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
                jumpAction.AddBinding("<Gamepad>/buttonSouth");
            }
        }

        private void OnEnable()
        {
            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            lookAction.Disable();
            jumpAction.Disable();
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
        }

        /// <summary>
        /// Processes camera rotation based on the mouse look delta input.
        /// Rotates the camera vertically (pitch) and the body horizontally (yaw).
        /// </summary>
        private void HandleLook()
        {
            if (playerCamera == null) return;

            Vector2 lookInput = lookAction.ReadValue<Vector2>();

            // Calculate rotations
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            // Rotate camera up and down (pitch)
            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);
            playerCamera.localRotation = Quaternion.Euler(_cameraPitch, 0.0f, 0.0f);

            // Rotate player body left and right (yaw)
            transform.Rotate(Vector3.up * mouseX);
        }

        /// <summary>
        /// Handles player movement input, gravity application, and jumping.
        /// Applies movement relative to the player's facing direction.
        /// </summary>
        private void HandleMovement()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                // Small negative value to keep player snapped to the ground
                _velocity.y = -2f;
            }

            // Read 2D movement input
            Vector2 moveInput = moveAction.ReadValue<Vector2>();

            // Convert to 3D movement relative to player rotation
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            _characterController.Move(moveDirection * (walkSpeed * Time.deltaTime));

            // Handle jumping
            if (jumpAction.triggered && _isGrounded)
            {
                // Physics formula for velocity to reach target jump height: v = sqrt(h * -2 * g)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                Debug.Log("[PlayerController] Player Jumped.");
            }

            // Apply gravity over time
            _velocity.y += gravity * Time.deltaTime;

            // Apply velocity vector (including gravity and jumping)
            _characterController.Move(_velocity * Time.deltaTime);
        }

        /// <summary>
        /// External setter to dynamically adjust mouse sensitivity (e.g. from pause/settings menus).
        /// </summary>
        /// <param name="sensitivity">New look sensitivity.</param>
        public void SetSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
    }
}
