using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]

public class Camera_Persp_Change : MonoBehaviour
{
    private Camera cam;
    private bool isOrthographic = false;
    private Coroutine blendCoroutine;

    [Header("Transition Settings")]
    public float duration = 1.0f;
    [Range(0, 1)] public float currentT = 0f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        isOrthographic = cam.orthographic;
    }

    public void OnPerspchange(InputValue value)
    {
        if (value.isPressed)
        {
            ToggleProjection();
        }
    }

    [SerializeField] private Transform targetObject;

    Vector3 targetPosition;
    Vector3 cameraPosition = new Vector3(50f, 40f, 100f);
    void Update()
    {
        if (targetObject != null)
        {
            // Access the position vector (X, Y, Z)
            targetPosition = targetObject.position;

            Debug.Log("Target Position: " + targetPosition);
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

        while (time < duration)
        {

            time += Time.deltaTime;
            float rawT = Mathf.Clamp01(time / duration);



            // SmoothStep yields cleaner transitions on matrix curves than linear lerping
            float smoothedT = Mathf.SmoothStep(startT, targetT, rawT);
            currentT = smoothedT;

            //change cam rot & pos
            transform.rotation = Quaternion.Euler(0, -90 * smoothedT, 0);
            transform.position = VectorLerp(targetPosition, cameraPosition, smoothedT);

            // Linearly interpolate the individual values inside the matrix row by row
            cam.projectionMatrix = MatrixLerp(perspectiveMatrix, orthoMatrix, smoothedT);
            yield return null;
        }

        // Snap fully to native settings at the end of the transition to clear cache/artifacts
        currentT = targetT;
        cam.orthographic = isOrthographic;
        cam.ResetProjectionMatrix();
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
