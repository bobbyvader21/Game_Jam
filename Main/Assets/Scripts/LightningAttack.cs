using UnityEngine;
using UnityEngine.InputSystem;

public class LightningAttack : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public Camera_Persp_Change projection;
    public Transform handsRoot;
    public Transform leftOrigin;
    public Transform rightOrigin;
    public Material lightningMaterial;

    [Header("Attack")]
    public LayerMask hitMask;
    public float range = 20f;
    public float damagePerSecond = 35f;

    [Header("Knockback")]
    public float knockbackImpulse = 2f;
    public float knockbackInterval = 0.2f;

    private float nextKnockbackTime;

    [Header("Appearance")]
    public float width = 0.015f;
    public float jitter = 0.025f;

    [Range(3, 30)]
    public int segments = 12;

    private LineRenderer leftBolt;
    private LineRenderer rightBolt;

    private bool CanAttack =>
        aimCamera != null &&
        projection != null &&
        !projection.isOrthographic &&
        projection.currentT <= 0.001f;

    private void Awake()
    {
        leftBolt = CreateBolt("Left Lightning");
        rightBolt = CreateBolt("Right Lightning");
    }

    private LineRenderer CreateBolt(string objectName)
    {
        GameObject boltObject = new GameObject(objectName);
        boltObject.transform.SetParent(transform, false);

        LineRenderer line = boltObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.sharedMaterial = lightningMaterial;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        return line;
    }

    private void LateUpdate()
    {
        bool ready = CanAttack;

        if (handsRoot != null)
            handsRoot.gameObject.SetActive(ready);

        bool firing =
            ready &&
            Mouse.current != null &&
            Mouse.current.leftButton.isPressed &&
            Cursor.lockState == CursorLockMode.Locked;

        leftBolt.enabled = firing && leftOrigin != null;
        rightBolt.enabled = firing && rightOrigin != null;

        if (!firing)
            return;

        Ray aimRay = aimCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Vector3 aimPoint = aimRay.GetPoint(range);

        if (Physics.Raycast(
            aimRay,
            out RaycastHit aimHit,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore))
        {
            aimPoint = aimHit.point;
        }

        Transform origin =
            rightOrigin != null ? rightOrigin : leftOrigin;

        if (origin == null)
            return;

        Vector3 direction = aimPoint - origin.position;
        Vector3 impactPoint = aimPoint;

        // Check for cover between the firing hand and the aimed point.
        if (direction.sqrMagnitude > 0.000001f &&
            Physics.Raycast(
                origin.position,
                direction.normalized,
                out RaycastHit impact,
                direction.magnitude + 0.005f,
                hitMask,
                QueryTriggerInteraction.Ignore))
        {
            impactPoint = impact.point;

            GameObject target = impact.collider.gameObject;

            if (target != null)
            {
                if (target.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damagePerSecond * Time.deltaTime);
                }
                if (target.TryGetComponent(out Rigidbody rigidbody) && !rigidbody.isKinematic && Time.time >= nextKnockbackTime)
                {
                    Vector3 pushDirection = aimRay.direction;
                    // Push along the floor rather than into it.
                    pushDirection.y = 0f;
                    if (pushDirection.sqrMagnitude > 0.0001f)
                    {
                        rigidbody.AddForce(
                            pushDirection.normalized * knockbackImpulse,
                            ForceMode.Impulse
                        );
                        nextKnockbackTime =
                            Time.time + Mathf.Max(0.01f, knockbackInterval);
                    }
                }

            }
        }

        if (leftOrigin != null)
            DrawBolt(leftBolt, leftOrigin.position, impactPoint);

        if (rightOrigin != null)
            DrawBolt(rightBolt, rightOrigin.position, impactPoint);
    }

    private void DrawBolt(
        LineRenderer line,
        Vector3 start,
        Vector3 end)
    {
        int count = Mathf.Max(3, segments);
        line.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            if (i > 0 && i < count - 1)
                point += Random.insideUnitSphere * jitter;

            line.SetPosition(i, point);
        }
    }

    private void OnDisable()
    {
        if (leftBolt != null)
            leftBolt.enabled = false;

        if (rightBolt != null)
            rightBolt.enabled = false;

        if (handsRoot != null)
            handsRoot.gameObject.SetActive(false);
    }
}