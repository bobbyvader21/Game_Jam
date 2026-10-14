using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class Camera_Persp_Change : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    [SerializeField] private Transform targetObject;
    public playerController playerControllerRef; // Assuming this is your custom script

    [Header("Camera Settings")]
    public bool isOrthographic = false;
    [SerializeField] private Vector3 CamOffset3D = Vector3.zero;
    [SerializeField] private Vector3 CameraOrthoOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float DeadpanRadius = 1.5f;
    [SerializeField] public float mouseSensitivity = 50f;

    [Header("Transition Settings")]
    public float duration = 1f;
    [Range(0f, 1f)] public float currentT = 0f;

    // Internal State
    private Camera cam;
    private CameraControls controls;
    private Coroutine blendCoroutine;
    private bool isTransitioning;

    private Vector3 currentVelocity = Vector3.zero;
    private Vector2 lookInput;
    public float xRotation = 0f;
    public float yRotation = 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        controls = new CameraControls();

        isOrthographic = cam.orthographic;
        currentT = isOrthographic ? 1f : 0f;

        // Preserve initial rotation
        Vector3 initialRotation = transform.eulerAngles;
        xRotation = Mathf.DeltaAngle(0f, initialRotation.x);
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        yRotation = initialRotation.y;
    }

    private void Start()
    {
        if (targetObject == null)
            targetObject = playerBody;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize directly in the Camera's starting mode
        if (isOrthographic)
        {
            transform.position = targetObject.position + CameraOrthoOffset;
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            transform.position = targetObject.position + CamOffset3D;
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
    private void OnDestroy() => controls?.Dispose();

    private void Update()
    {
        if (targetObject == null || isOrthographic || isTransitioning)
            return;

        lookInput = controls.CamLook3D.CamMouse.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;
        yRotation %= 360f;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void LateUpdate()
    {
        if (targetObject == null || isTransitioning) return;

        if (isOrthographic)
        {
            // 1. Flatten current camera and target positions to the ZY plane
            Vector2 cam2D = new Vector2(transform.position.z, transform.position.y);
            Vector2 target2D = new Vector2(targetObject.position.z, targetObject.position.y);

            // 2. Calculate the flat 2D distance
            float distance = Vector2.Distance(cam2D, target2D);

            if (distance > DeadpanRadius)
            {
                // 3. Get the flat 2D direction vector from camera to target
                Vector2 direction2D = (target2D - cam2D).normalized;

                // 4. Calculate the target 2D position just outside the deadzone
                Vector2 desired2D = target2D - (direction2D * (DeadpanRadius + 0.1f)) + new Vector2(CameraOrthoOffset.z, CameraOrthoOffset.y);

                // 5. Reconstruct the full 3D target position (Keep current X position, or map CameraOrthoOffset.x if needed)
                Vector3 desiredPosition = new Vector3(
                    transform.position.x,// + CameraOrthoOffset.x,
                    desired2D.y,
                    desired2D.x
                );

                // 6. Smoothly damp to the new 3D position
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref currentVelocity,
                    Mathf.Max(0.001f, smoothTime)
                );
            }
            else
            {
                // Keep the velocity at zero, but you can also damp the remaining tiny movement here if needed
                currentVelocity = Vector3.zero;
            }
        }
        else
        {
            // 3D direct tracking
            transform.position = targetObject.position + CamOffset3D;
        }
    }

    public void OnPerspchange(InputValue value)
    {
        if (value.isPressed)
            ToggleProjection();
    }

    public void ToggleProjection()
    {
        if (targetObject == null) return;

        if (blendCoroutine != null)
            StopCoroutine(blendCoroutine);

        isOrthographic = !isOrthographic;
        currentVelocity = Vector3.zero;

        blendCoroutine = StartCoroutine(BlendProjection(isOrthographic ? 1f : 0f));
    }

    private IEnumerator BlendProjection(float targetT)
    {
        if (!isOrthographic)
        {
            xRotation = 0.0f;
            yRotation = 0.0f + (playerControllerRef.playerDirection) * -180;
        }
        isTransitioning = true;

        float startT = currentT;
        float elapsed = 0f;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Matrix4x4 startProjection = cam.projectionMatrix;

        Matrix4x4 endProjection = isOrthographic ? GetOrthoMatrix() : GetPerspectiveMatrix();
        Quaternion endRotation = isOrthographic ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(xRotation, yRotation, 0f);

        float transitionDuration = Mathf.Max(0.001f, duration);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            currentT = Mathf.Lerp(startT, targetT, smoothProgress);

            Vector3 endPosition = isOrthographic
                ? targetObject.position + CameraOrthoOffset
                : targetObject.position + CamOffset3D;

            transform.position = Vector3.Lerp(startPosition, endPosition, smoothProgress);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, smoothProgress);
            cam.projectionMatrix = MatrixLerp(startProjection, endProjection, smoothProgress);

            yield return null;
        }

        currentT = targetT;
        cam.orthographic = isOrthographic;
        cam.ResetProjectionMatrix();

        transform.position = isOrthographic ? targetObject.position + CameraOrthoOffset : targetObject.position + CamOffset3D;
        transform.rotation = endRotation;

        isTransitioning = false;
        blendCoroutine = null;

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
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = Mathf.Lerp(from[i], to[i], t);
        }
        return result;
    }
}