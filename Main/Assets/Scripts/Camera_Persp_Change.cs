using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class Camera_Persp_Change : MonoBehaviour
{
    private Camera cam;
    public bool isOrthographic = false;
    private Coroutine blendCoroutine;
    private bool isTransitioning = false; // Prevents LateUpdate conflicts

    [Header("Transition Settings")]
    public float duration = 1.0f;
    [Range(0, 1)] public float currentT = 0f;

    [Header("Camera Settings")]
    public Vector3 CameraOrthoOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float DeadpanRadius = 1.5f;
    [SerializeField] private Vector3 CamOffset3D = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;

    private CameraControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yRotation = 0f;
    public playerController playerControllerRef;

    [SerializeField] public float mouseSensitivity = 50f;
    public Transform playerBody;

    [SerializeField] private Transform targetObject;
    private Vector3 targetPosition;
    private Vector3 cameraPosition;

    float dist(float x1, float y1, float x2, float y2)
    {
        float xdif = x2 - x1;
        float ydif = y2 - y1;
        return Mathf.Sqrt(xdif * xdif + ydif * ydif);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        isOrthographic = cam.orthographic;
        controls = new CameraControls();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        isOrthographic = !isOrthographic;
        ToggleProjection();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        lookInput = controls.CamLook3D.CamMouse.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        if (targetObject != null)
        {
            targetPosition = targetObject.position;
        }

        if (isOrthographic)
        {
            cameraPosition = targetPosition + CameraOrthoOffset;
        }
        else if (!isTransitioning) // Avoid messing with rotation properties mid-transition
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            yRotation += mouseX;
            yRotation = yRotation % 360;
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    private void LateUpdate()
    {
        // Block positioning logic if the coroutine is blending positions
        if (isTransitioning) return;

        if (targetObject != null && isOrthographic)
        {
            float camx = transform.position.z;
            float camy = transform.position.y;
            float tpx = targetObject.position.z;
            float tpy = targetObject.position.y;

            Vector3 targetPos;
            if (dist(camx, camy, tpx, tpy) > DeadpanRadius)
            {
                targetPos = targetObject.position + CameraOrthoOffset;
            }
            else
            {
                targetPos = transform.position;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
        }
        else
        {
            if (targetObject != null && !cam.orthographic)
                transform.position = targetObject.position + CamOffset3D;
        }
    }

    public void OnPerspchange(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleProjection();
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
        isTransitioning = true;
        float startT = currentT;
        float time = 0;

        Matrix4x4 perspectiveMatrix = GetPerspectiveMatrix();
        Matrix4x4 orthoMatrix = GetOrthoMatrix();
        Vector3 PrevCamRot = new Vector3(xRotation, yRotation, 0.0f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float rawT = Mathf.Clamp01(time / duration);
            float smoothedT = Mathf.SmoothStep(startT, targetT, rawT);
            currentT = smoothedT;

            // Rotation blend
            transform.rotation = Quaternion.Euler(PrevCamRot.x * (1 - smoothedT), (PrevCamRot.y * (1 - smoothedT)) + (-90 * smoothedT), 0);

            // Position blend
            transform.position = Vector3.Lerp(targetPosition + CamOffset3D, cameraPosition, smoothedT);

            // Matrix blend
            cam.projectionMatrix = MatrixLerp(perspectiveMatrix, orthoMatrix, smoothedT);
            yield return null;
        }

        currentT = targetT;
        cam.orthographic = isOrthographic;
        cam.ResetProjectionMatrix(); // Safe to reset now that state reflects reality

        // Reset tracking vars cleanly for first-person look setup
        xRotation = 0.0f;
        yRotation = 0.0f;
        isTransitioning = false;
    }

    private Matrix4x4 GetPerspectiveMatrix()
    {
        return Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
    }

    private Matrix4x4 GetOrthoMatrix()
    {
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
}
