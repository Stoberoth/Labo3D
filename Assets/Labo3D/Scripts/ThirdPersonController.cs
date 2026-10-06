using UnityEngine;
using UnityEngine.InputSystem;

namespace Labo3D
{
    /// <summary>
    /// Déplacement troisième personne. La souris orbite, ZQSD (WASD physique)
    /// avance par rapport à l'orientation horizontale de la caméra.
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
            // Le clic dans la vue Jeu capture la souris. On ne la prend pas
            // dès l'entrée en lecture, sinon l'éditeur vole le curseur.
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
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
                Look(mouse);

            Move(keyboard);
        }

        void LateUpdate()
        {
            PlaceCamera();
        }

        void Look(Mouse mouse)
        {
            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
        }

        void Move(Keyboard keyboard)
        {
            Vector2 move = ReadMove(keyboard);
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

            if (body.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
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

        static Vector2 ReadMove(Keyboard keyboard)
        {
            if (keyboard == null)
                return Vector2.zero;

            // Key.W / A / S / D sont les positions physiques d'un clavier QWERTY.
            // Sur un AZERTY, ces mêmes touches écrivent Z Q S D.
            float x = 0f;
            float y = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y);
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
