using UnityEngine;

namespace AdonisLife.Gameplay
{
    /// <summary>
    /// Pure player movement math: converts raw input into a world-space displacement with
    /// normalized diagonals, sprint multiplier, and constant gravity.
    /// </summary>
    public static class PlayerMovementModel
    {
        public const float Gravity = -20f;

        /// <summary>
        /// Horizontal displacement for one frame. Input is clamped to unit length so diagonal
        /// movement is not faster.
        /// </summary>
        public static Vector3 ComputeMove(
            Vector2 input, float yawDegrees, float walkSpeed, float sprintMultiplier, bool sprinting, float deltaTime)
        {
            if (input.sqrMagnitude > 1f)
            {
                input = input.normalized;
            }

            float speed = walkSpeed * (sprinting ? sprintMultiplier : 1f);
            Quaternion yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 direction = yaw * new Vector3(input.x, 0f, input.y);
            return direction * speed * deltaTime;
        }

        /// <summary>Vertical velocity after one frame of gravity, zeroed while grounded.</summary>
        public static float ApplyGravity(float verticalVelocity, bool grounded, float deltaTime)
        {
            if (grounded && verticalVelocity < 0f)
            {
                return -1f;
            }

            return verticalVelocity + Gravity * deltaTime;
        }
    }

    /// <summary>
    /// Runtime third-person player controller driven by a CharacterController. All movement
    /// math lives in <see cref="PlayerMovementModel"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintMultiplier = 1.8f;
        [SerializeField] private float _turnSpeedDegrees = 120f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private IPlayerInputSource _inputSource;

        public float WalkSpeed => _walkSpeed;

        /// <summary>Input source; defaults to the keyboard, injectable for tests.</summary>
        public IPlayerInputSource InputSource
        {
            get => _inputSource ?? (_inputSource = new KeyboardInputSource());
            set => _inputSource = value;
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = InputSource.MoveInput;
            bool sprinting = InputSource.Sprint;

            // Forward input moves along the player's facing; horizontal input turns.
            transform.Rotate(0f, input.x * _turnSpeedDegrees * Time.deltaTime, 0f);
            Vector3 move = PlayerMovementModel.ComputeMove(
                new Vector2(0f, Mathf.Max(0f, input.y)), transform.eulerAngles.y,
                _walkSpeed, _sprintMultiplier, sprinting, Time.deltaTime);

            _verticalVelocity = PlayerMovementModel.ApplyGravity(
                _verticalVelocity, _characterController.isGrounded, Time.deltaTime);
            move.y = _verticalVelocity * Time.deltaTime;

            _characterController.Move(move);
        }
    }
}
