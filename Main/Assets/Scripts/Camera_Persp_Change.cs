using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class Camera_Persp_Change : MonoBehaviour
{
    private Camera cam;
    private CameraControls controls;
    private Coroutine blendCoroutine;
    private bool isTransitioning;

    public bool isOrthographic = false;

    [Header("Transition Settings")]
    public float duration = 1f;

    [Range(0f, 1f)]
    public float currentT = 0f;

    [Header("Camera Settings")]
    public Vector3 CameraOrthoOffset = new Vector3(2f, 0f, 0f);

    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float DeadpanRadius = 1.5f;

    private Vector3 currentVelocity;

    public playerController playerControllerRef;

    [SerializeField] public float mouseSensitivity = 50f;

    public Transform playerBody;

    [SerializeField] private Transform targetObject;

    private Vector2 lookInput;
    private float xRotation;
    private float yRotation;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        controls = new CameraControls();

        isOrthographic = cam.orthographic;
        currentT = isOrthographic ? 1f : 0f;

        // Preserve the initial perspective look direction.
        Vector3 initialRotation = transform.eulerAngles;

        xRotation = Mathf.DeltaAngle(0f, initialRotation.x);
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation = initialRotation.y;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();

        if (blendCoroutine != null)
        {
            StopCoroutine(blendCoroutine);
            blendCoroutine = null;
        }

        isTransitioning = false;

        // Restore a native projection if disabled during a transition.
        if (cam != null)
        {
            cam.orthographic = isOrthographic;
            currentT = isOrthographic ? 1f : 0f;
            cam.ResetProjectionMatrix();
        }
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }

    private void Start()
    {
        if (targetObject == null)
            targetObject = playerBody;

        if (targetObject == null)
        {
            Debug.LogError(
                "Camera_Persp_Change: Assign Target Object or Player Body.",
                this
            );

            enabled = false;
            return;
        }

        // Initialize directly in the Camera component's selected mode.
        if (isOrthographic)
        {
            transform.position =
                targetObject.position + CameraOrthoOffset;

            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            transform.position = targetObject.position;
            transform.rotation =
                Quaternion.Euler(xRotation, yRotation, 0f);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnPerspchange(InputValue value)
    {
        if (value.isPressed)
            ToggleProjection();
    }

    private void Update()
    {
        if (targetObject == null ||
            isOrthographic ||
            isTransitioning)
        {
            return;
        }

        lookInput = controls.CamLook3D.CamMouse.ReadValue<Vector2>();

        float mouseX =
            lookInput.x * mouseSensitivity * Time.deltaTime;

        float mouseY =
            lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;
        yRotation %= 360f;

        transform.rotation =
            Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void LateUpdate()
    {
        if (targetObject == null || isTransitioning)
            return;

        if (isOrthographic)
        {
            float distance = Vector2.Distance(
                new Vector2(
                    transform.position.z,
                    transform.position.y
                ),
                new Vector2(
                    targetObject.position.z,
                    targetObject.position.y
                )
            );

            if (distance > DeadpanRadius)
            {
                Vector3 desiredPosition =
                    targetObject.position + CameraOrthoOffset;

                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref currentVelocity,
                    Mathf.Max(0.001f, smoothTime)
                );
            }
            else
            {
                currentVelocity = Vector3.zero;
            }
        }
        else
        {
            transform.position = targetObject.position;
        }
    }

    public void ToggleProjection()
    {
        if (targetObject == null)
            return;

        if (blendCoroutine != null)
            StopCoroutine(blendCoroutine);

        isOrthographic = !isOrthographic;
        currentVelocity = Vector3.zero;

        blendCoroutine = StartCoroutine(
            BlendProjection(isOrthographic ? 1f : 0f)
        );
    }

    private IEnumerator BlendProjection(float targetT)
    {
        isTransitioning = true;

        float startT = currentT;
        float elapsed = 0f;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Matrix4x4 startProjection = cam.projectionMatrix;

        Matrix4x4 endProjection = isOrthographic
            ? GetOrthoMatrix()
            : GetPerspectiveMatrix();

        Quaternion endRotation = isOrthographic
            ? Quaternion.Euler(0f, -90f, 0f)
            : Quaternion.Euler(xRotation, yRotation, 0f);

        float transitionDuration = Mathf.Max(0.001f, duration);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / transitionDuration
            );

            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            currentT = Mathf.Lerp(
                startT,
                targetT,
                smoothProgress
            );

            Vector3 endPosition = isOrthographic
                ? targetObject.position + CameraOrthoOffset
                : targetObject.position;

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                smoothProgress
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                smoothProgress
            );

            cam.projectionMatrix = MatrixLerp(
                startProjection,
                endProjection,
                smoothProgress
            );

            yield return null;
        }

        currentT = targetT;

        cam.orthographic = isOrthographic;
        cam.ResetProjectionMatrix();

        transform.position = isOrthographic
            ? targetObject.position + CameraOrthoOffset
            : targetObject.position;

        transform.rotation = endRotation;

        isTransitioning = false;
        blendCoroutine = null;
    }

    private Matrix4x4 GetPerspectiveMatrix()
    {
        return Matrix4x4.Perspective(
            cam.fieldOfView,
            cam.aspect,
            cam.nearClipPlane,
            cam.farClipPlane
        );
    }

    private Matrix4x4 GetOrthoMatrix()
    {
        float orthoHeight = cam.orthographicSize;
        float orthoWidth = orthoHeight * cam.aspect;

        return Matrix4x4.Ortho(
            -orthoWidth,
            orthoWidth,
            -orthoHeight,
            orthoHeight,
            cam.nearClipPlane,
            cam.farClipPlane
        );
    }

    private Matrix4x4 MatrixLerp(
        Matrix4x4 from,
        Matrix4x4 to,
        float t)
    {
        Matrix4x4 result = new Matrix4x4();

        for (int i = 0; i < 16; i++)
            result[i] = Mathf.Lerp(from[i], to[i], t);

        return result;
    }
}