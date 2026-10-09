using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float friction = 1.0f;
    [SerializeField] private float movementSpeed = 1.0f;
    [SerializeField] public float jumpHeight = 1.0f;

    private bool canMove = true;
    private Vector2 inputVector;
    private Vector3 moveValue;
    private Rigidbody rb;

    [SerializeField] private InputActionReference moveActionReference;

    public bool CanMove
    {
        get => canMove;
        set => canMove = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (moveActionReference != null)
        {
            moveActionReference.action.Enable();
            moveActionReference.action.performed += MovePlayer;
            moveActionReference.action.canceled += MovePlayer;
        }
    }

    private void OnDisable()
    {
        if (moveActionReference != null)
        {
            moveActionReference.action.performed -= MovePlayer;
            moveActionReference.action.canceled -= MovePlayer;
            moveActionReference.action.Disable();
        }
    }

    public void MovePlayer(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>();
        moveValue = new Vector3(inputVector.y, 0.0f, inputVector.x);
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            // Multiplying by Time.fixedDeltaTime inside AddForce with ForceMode.Force 
            // applies smooth, frame-rate independent movement.
            rb.AddForce(moveValue * movementSpeed, ForceMode.Force);
        }
    }
}
