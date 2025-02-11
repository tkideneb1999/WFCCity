using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 0.5f;
        public float mouseSensitivity = 0.5f;
        public float gravity = -9.81f;
        public Vector3 groundOffset;
        public Transform playerCamera;

        private Controls _controls;
        private CharacterController _controller;

        private Vector3 _gravityMove;
        private bool _isGrounded;

        private float _xRotation = 0.0f;
        // Start is called before the first frame update
        void Awake()
        {
            _controls = new Controls();
            _controls.Player.Enable();
            _controller = GetComponent<CharacterController>();
            
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            _isGrounded = Physics.CheckSphere(transform.position - groundOffset, 0.4f);

            if(!_isGrounded)
            {
                _gravityMove.y += gravity * Time.fixedDeltaTime;
            }
            else
            {
                _gravityMove.y = gravity * Time.fixedDeltaTime;
            }
            Vector2 moveInput = _controls.Player.Movement.ReadValue<Vector2>();
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            Vector3 moveDir = (moveInput.y * forward) + (moveInput.x * right);
            _controller.Move(moveDir * speed + _gravityMove);
        }

        private void Update()
        {
            Vector2 camInput = _controls.Player.Camera.ReadValue<Vector2>() * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up, camInput.x);

            _xRotation -= camInput.y;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }
}
