using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float JumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;
    private PlayerMovement PlayerControls;
    private Vector2 movement;
    private Rigidbody rigidBody;

    private void Awake()
    {
        PlayerControls = new PlayerMovement();
        rigidBody = GetComponent<Rigidbody>();

        PlayerControls.Player2D.Jump.performed += OnJump;
    }

    private void OnEnable()
    {
        PlayerControls.Enable();
    }
    private void OnDisable()
    {
        PlayerControls.Disable();
    }



    private void PlayerInput()
    {
        movement = PlayerControls.Player2D.Move.ReadValue<Vector2>();
    }
    private void checkIsGrounded()
    {
        isGrounded = (Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer).Length > 0) ? true : false;
    }

    private void FixedUpdate()
    {
        Move();
    }
    private void Update()
    {
        PlayerInput();
        checkIsGrounded();
    }

    private void Move()
    {
        rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x,rigidBody.linearVelocity.y, -1 * movement.x * moveSpeed);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, JumpForce, rigidBody.linearVelocity.z);
        }
        
    }
}
