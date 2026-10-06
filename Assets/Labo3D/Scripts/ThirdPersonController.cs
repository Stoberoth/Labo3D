using UnityEngine;
using UnityEngine.InputSystem;

namespace Labo3D
{
    /// <summary>
    /// Déplacement troisième personne. La souris orbite, le déplacement et le
    /// saut viennent des actions projet Player/Move, Player/Look et Player/Jump.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float jumpHeight = 1.25f;
        [SerializeField] float gravity = -25f;
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float minPitch = -25f;
        [SerializeField] float maxPitch = 55f;
        [SerializeField] float cameraDistance = 6.5f;
        [SerializeField] float focusHeight = 1.4f;
        [SerializeField] float turnSpeed = 12f;

        CharacterController body;
        InputAction moveAction;
        InputAction lookAction;
        InputAction jumpAction;
        float verticalVelocity;
        float yaw;
        float pitch = 18f;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (viewCamera == null)
                viewCamera = Camera.main;
        }

        void OnEnable()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError(
                    "ThirdPersonController : InputSystem.actions est vide. Assigne InputSystem_Actions dans Project Settings > Input System Package.",
                    this);
                return;
            }

            moveAction = actions.FindAction("Player/Move", throwIfNotFound: false);
            lookAction = actions.FindAction("Player/Look", throwIfNotFound: false);
            jumpAction = actions.FindAction("Player/Jump", throwIfNotFound: false);

            if (moveAction == null || lookAction == null || jumpAction == null)
            {
                Debug.LogError(
                    "ThirdPersonController : la map Player doit exposer Move, Look et Jump.",
                    this);
            }
        }

        void OnDisable()
        {
            moveAction = null;
            lookAction = null;
            jumpAction = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            // La capture du curseur reste locale à la vue Jeu. Ce n'est pas un déplacement.
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                LockCursor();

            if (Cursor.lockState == CursorLockMode.Locked)
                Look();

            Move();
        }

        void LateUpdate()
        {
            PlaceCamera();
        }

        void Look()
        {
            if (lookAction == null)
                return;

            Vector2 delta = lookAction.ReadValue<Vector2>();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
        }

        void Move()
        {
            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 wish = yawRotation * new Vector3(move.x, 0f, move.y);
            if (wish.sqrMagnitude > 1f)
                wish.Normalize();

            if (wish.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, turnSpeed * Time.deltaTime);
            }

            // isGrounded n'est fiable qu'après un Move. Le -2 colle le corps au sol.
            if (body.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (body.isGrounded && jumpAction != null && jumpAction.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = wish * moveSpeed;
            velocity.y = verticalVelocity;
            body.Move(velocity * Time.deltaTime);
        }

        void PlaceCamera()
        {
            if (viewCamera == null)
                return;

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = transform.position + Vector3.up * focusHeight;
            Vector3 position = focus + orbit * new Vector3(0f, 0f, -cameraDistance);
            viewCamera.transform.SetPositionAndRotation(position, orbit);
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
