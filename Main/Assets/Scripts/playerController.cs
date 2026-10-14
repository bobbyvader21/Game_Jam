using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerController : MonoBehaviour
{
    
    [Header("Camera Reference")]
    [SerializeField] private Transform Cam;
    public Camera_Persp_Change Camera_Persp_ChangeRef;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float JumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Better Jump")]
    [SerializeField] private float fallMultiplier = 2.5f;
    private bool isGrounded;
    private PlayerMovement PlayerControls;
    private Vector2 movement2D;
    private Vector3 movement3D;
    private Rigidbody rigidBody;
    private Collider playerCollider;
    private float jumpBufferTime = 0f;

    public Vector3 linearVelocity = new Vector3(0f, 0f, 0f);
    public int playerDirection;

    private void Awake()
    {
        PlayerControls = new PlayerMovement();
        rigidBody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        //PlayerControls.Player2D.Jump.performed += OnJump;
    }

    private void OnEnable()
    {
        PlayerControls.Enable();
    }
    private void OnDisable()
    {
        PlayerControls.Disable();
    }

    private void Start()
    {
        playerDirection = 0;
    }

    private void PlayerInput(bool isOrtho)
    {
        if (isOrtho)
        {
            movement2D = PlayerControls.Player2D.Move.ReadValue<Vector2>();
            rigidBody.constraints = RigidbodyConstraints.FreezePositionX |
                                    RigidbodyConstraints.FreezeRotationX |
                                    RigidbodyConstraints.FreezeRotationY |
                                    RigidbodyConstraints.FreezeRotationZ;
        } else
        {
            movement3D = PlayerControls.Player3D.Move.ReadValue<Vector3>();
            rigidBody.constraints &= ~RigidbodyConstraints.FreezePositionX;
        }
    }
    private void checkIsGrounded()
    {
        isGrounded = (Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer).Length > 0) ? true : false;
    }

    

    private void FixedUpdate()
    {
        Move(Camera_Persp_ChangeRef.isOrthographic);
        handleBetterFall();
        if (isGrounded)
        {
            jumpBufferTime = 0;
        }
        else
        {
            jumpBufferTime += Time.deltaTime;
        }
        //Debug.Log(jumpBufferTime);
        
    }
    private void Update()
    {
        PlayerInput(Camera_Persp_ChangeRef.isOrthographic);
        checkIsGrounded();
        camAngle = getCamAngle();
        if (rigidBody)
        {
            linearVelocity = rigidBody.linearVelocity;
        }
    }
    private Vector3 camAngle;
    private Vector3 getCamAngle()
    {
        if(Cam != null)
        {
            Vector3 camAngle = Cam.eulerAngles;
            //Debug.Log($"Rotation in Degrees: {camAngle}");
            return camAngle;
        }
        return new Vector3(0f, 0f, 0f);
    }
    private void Move(bool isOrtho)
    {
        if (isOrtho)
        {
            if (Camera_Persp_ChangeRef.isOrthographic && (jumpBufferTime < 0.1))
            {
                if (movement2D.y > 0)
                {
                    rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, JumpForce, rigidBody.linearVelocity.z);
                    jumpBufferTime += 100;
                }
            }
            if (movement2D.magnitude == 0 && isGrounded)
            {
                // Stand firm on slopes
                playerCollider.material.dynamicFriction = 1f;
                playerCollider.material.staticFriction = 1f;
                playerCollider.material.frictionCombine = PhysicsMaterialCombine.Maximum;
            }
            else
            {
                // Slide freely past walls
                playerCollider.material.dynamicFriction = 0f;
                playerCollider.material.staticFriction = 0f;
                playerCollider.material.frictionCombine = PhysicsMaterialCombine.Minimum;
            }

            if(movement2D.x == 1)
            {
                playerDirection = 1;
            } else if(movement2D.x == -1)
            {
                playerDirection = 0;
            }
            rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, rigidBody.linearVelocity.y, -1 * movement2D.x * moveSpeed);
            //Debug.Log(movement2D);
        }
        else
        {
            float radians = camAngle.y * Mathf.Deg2Rad;
            float camSin = Mathf.Sin(radians);
            float camCos = Mathf.Cos(radians);

            float xmove = movement3D.x * camCos + movement3D.z * camSin;
            float zmove = -movement3D.x * camSin + movement3D.z * camCos;

            rigidBody.linearVelocity = new Vector3(xmove * moveSpeed, rigidBody.linearVelocity.y, zmove * moveSpeed);
            //Debug.Log(Camera_Persp_ChangeRef.yRotation);
            float yrot = Camera_Persp_ChangeRef.yRotation;
            if (((yrot < -90) && (yrot > -270)) || ((yrot) > 90) && (yrot < 270))
            {
                playerDirection = 1;
            }
            else 
            {
                playerDirection = 0;
            }
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && Camera_Persp_ChangeRef.isOrthographic)
        {
            rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, JumpForce, rigidBody.linearVelocity.z);
        }
        
    }
    private void handleBetterFall()
    {
        if (rigidBody.linearVelocity.y < 0)
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        }
    }
}
