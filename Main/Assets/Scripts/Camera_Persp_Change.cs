using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Camera))]

public class Camera_Persp_Change : MonoBehaviour
{
    private Camera cam;
    public bool isOrthographic = false;
    private Coroutine blendCoroutine;

    [Header("Transition Settings")]
    public float duration = 1.0f;
    [Range(0, 1)] public float currentT = 0f;

    [Header("Camera Settings")]
    public Vector3 CameraOrthoOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float DeadpanRadius = 1.5f;// Approximate time to reach target
    private Vector3 currentVelocity = Vector3.zero;
    float dist(float x1, float y1,float x2, float y2)
    {
        float xdif = x2 - x1;
        float ydif = y2 - y1;
        return Mathf.Sqrt(xdif * xdif + ydif * ydif);
    }
    private void LateUpdate()
    {
        if (targetObject != null && isOrthographic)
        {
            float camx = transform.position.z;
            float camy = transform.position.y;
            float tpx = targetObject.position.z;
            float tpy = targetObject.position.y;
            Debug.Log(dist(camx, camy, tpx, tpy));
            Debug.Log($"camx: {camx}, camy: {camy}, tpx: {tpx}, tpy: {tpy}");
            // Calculate target location with the relative offset
            Vector3 targetPosition;
            if (dist(camx, camy, tpx, tpy) > DeadpanRadius)
            {
                targetPosition = targetObject.position + CameraOrthoOffset;
            } else
            {
                targetPosition = transform.position;
            }

            // Smoothly glide the camera position toward the target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        } else
        {
            if(!cam.orthographic) transform.position = targetObject.position;
        }

        
    }

    private CameraControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yRotation = 0f;

    public playerController playerControllerRef;



    void Awake()
    {
        cam = GetComponent<Camera>();
        isOrthographic = cam.orthographic;
        controls = new CameraControls();
    }

    public void OnPerspchange(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleProjection();
        }
    }

    [SerializeField] public float mouseSensitivity = 50f;
    public Transform playerBody; // Assign your parent player object here if doing First Person




    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Lock cursor to the center of the screen and hide it
        isOrthographic = !isOrthographic;
        ToggleProjection();
        Cursor.lockState = CursorLockMode.Locked;
    }



    [SerializeField] private Transform targetObject;

    Vector3 targetPosition;
    Vector3 cameraPosition;
    Vector3 cameraRotation;
    void Update()
    {

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Read the Vector2 Delta value from the mouse
        lookInput = controls.CamLook3D.CamMouse.ReadValue<Vector2>();



        // Calculate vertical rotation and clamp it so you can't look upside down


        if (targetObject != null)
        {
            // Access the position vector (X, Y, Z)
            targetPosition = targetObject.position;
           
        }
        if (isOrthographic)
        {
            cameraPosition = targetPosition + CameraOrthoOffset;
        } else
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            yRotation += mouseX;
            yRotation = yRotation % 360;

            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            //transform.Rotate(Vector3.up * mouseX, Space.World);
        }





    }

    public void ToggleProjection()
    {
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        isOrthographic = !isOrthographic;
        blendCoroutine = StartCoroutine(BlendProjection(isOrthographic ? 1f : 0f));
    }

    private System.Collections.IEnumerator BlendProjection(float targetT)
    {
        float startT = currentT;
        float time = 0;

        // Calculate the base matrices for both modes
        Matrix4x4 perspectiveMatrix = GetPerspectiveMatrix();
        Matrix4x4 orthoMatrix = GetOrthoMatrix();
        Vector3 PrevCamRot = new Vector3(xRotation,yRotation,0.0f);

        while (time < duration)
        {

            time += Time.deltaTime;
            float rawT = Mathf.Clamp01(time / duration);



            // SmoothStep yields cleaner transitions on matrix curves than linear lerping
            float smoothedT = Mathf.SmoothStep(startT, targetT, rawT);
            currentT = smoothedT;

            //change cam rot & pos
            transform.rotation = Quaternion.Euler(PrevCamRot.x * (1-smoothedT), (PrevCamRot.y * (1-smoothedT)) + (-90 * smoothedT), 0);
            transform.position = VectorLerp(targetPosition, cameraPosition, smoothedT);

            // Linearly interpolate the individual values inside the matrix row by row
            cam.projectionMatrix = MatrixLerp(perspectiveMatrix, orthoMatrix, smoothedT);
            yield return null;
        }

        // Snap fully to native settings at the end of the transition to clear cache/artifacts
        currentT = targetT;
        cam.orthographic = isOrthographic;
        cam.ResetProjectionMatrix();
        xRotation = 0.0f;
        yRotation = 0.0f;
    }

    private Matrix4x4 GetPerspectiveMatrix()
    {
        // Re-creates the native matrix from current aspect, FOV, and near/far clip planes
        return Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
    }

    private Matrix4x4 GetOrthoMatrix()
    {
        // Re-creates the native matrix from current orthographic bounds
        float orthoHeight = cam.orthographicSize;
        float orthoWidth = orthoHeight * cam.aspect;
        return Matrix4x4.Ortho(-orthoWidth, orthoWidth, -orthoHeight, orthoHeight, cam.nearClipPlane, cam.farClipPlane);
    }

    private Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float t)
    {
        Matrix4x4 blended = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            blended[i] = Mathf.Lerp(from[i], to[i], t);
        }
        return blended;
    }

    private Vector3 VectorLerp(Vector3 from, Vector3 to, float t)
    {
        Vector3 blended = new Vector3();
        for (int i = 0; i < 3; i++)
        {
            blended[i] = Mathf.Lerp(from[i], to[i], t);
        }
        return blended;
    }
}
