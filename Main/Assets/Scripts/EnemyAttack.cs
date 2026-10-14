using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CombatHealth))]
public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Material beamMaterial;

    [Header("Ranged Attack — 3D Only")]
    public float attackRange = 8f;
    public float damage = 10f;
    public float attackInterval = 1f;
    public LayerMask sightMask;

    [Header("Visible Beam")]
    public float beamWidth = 0.02f;
    public float beamDuration = 0.15f;

    [Header("Contact Damage — Both Modes")]
    public float contactDamage = 10f;
    public float contactInterval = 1f;

    private CombatHealth health;
    private CombatHealth player;
    private Camera_Persp_Change projection;
    private Collider playerCollider;

    private LineRenderer beam;
    private Coroutine beamRoutine;

    private float nextAttackTime;
    private float nextContactTime;
    private bool wasAllowedToAttack;

    private void Awake()
    {
        health = GetComponent<CombatHealth>();

        GameObject beamObject = new GameObject("Enemy Beam");
        beamObject.transform.SetParent(transform, false);

        beam = beamObject.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.sharedMaterial = beamMaterial;
        beam.startWidth = beamWidth;
        beam.endWidth = beamWidth;
        beam.numCapVertices = 3;
        beam.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        beam.enabled = false;
    }

    private void Start()
    {
        CombatHealth[] allHealth =
            FindObjectsByType<CombatHealth>(
                FindObjectsInactive.Exclude
            );

        foreach (CombatHealth candidate in allHealth)
        {
            if (!candidate.isPlayer)
                continue;

            player = candidate;

            playerCollider = player.GetComponentInChildren<Collider>();

            projection = player.GetComponentInChildren<Camera_Persp_Change>();
            


            break;
        }

        if (player == null || projection == null)
        {
            Debug.LogWarning(
                $"{name}: Player health or camera was not found.",
                this
            );
        }
    }

    private bool CanShoot()
    {
        return
            !health.IsDead &&
            player != null &&
            !player.IsDead &&
            projection != null &&
            !projection.isOrthographic &&
            projection.currentT <= 0.001f;
    }

    private void Update()
    {
        if (!CanShoot())
        {
            wasAllowedToAttack = false;
            HideBeam();
            return;
        }

        // Brief delay after switching into 3D.
        if (!wasAllowedToAttack)
        {
            wasAllowedToAttack = true;

            nextAttackTime =
                Time.time + Mathf.Max(0.05f, attackInterval);
        }

        if (Time.time < nextAttackTime)
            return;

        Vector3 start = firePoint != null
            ? firePoint.position
            : transform.position;

        Vector3 targetPoint = playerCollider != null
            ? playerCollider.bounds.center
            : player.transform.position;

        Vector3 direction = targetPoint - start;
        float distance = direction.magnitude;

        if (distance <= 0.001f || distance > attackRange)
            return;

        if (!Physics.Raycast(
            start,
            direction.normalized,
            out RaycastHit hit,
            distance + 0.05f,
            sightMask,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        CombatHealth hitHealth =
            hit.collider.GetComponentInParent<CombatHealth>();

        // Only shoot when there is a clear line to the player.
        if (hitHealth != player)
            return;

        nextAttackTime =
            Time.time + Mathf.Max(0.05f, attackInterval);

        ShowBeam(start, hit.point);
        player.TakeDamage(damage);
    }

    private void ShowBeam(Vector3 start, Vector3 end)
    {
        if (beamRoutine != null)
            StopCoroutine(beamRoutine);

        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
        beam.enabled = true;

        beamRoutine = StartCoroutine(DisableBeamAfterDelay());
    }

    private IEnumerator DisableBeamAfterDelay()
    {
        yield return new WaitForSeconds(
            Mathf.Max(0.01f, beamDuration)
        );

        beam.enabled = false;
        beamRoutine = null;
    }

    private void HideBeam()
    {
        if (beamRoutine != null)
        {
            StopCoroutine(beamRoutine);
            beamRoutine = null;
        }

        if (beam != null)
            beam.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryContactDamage(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryContactDamage(collision);
    }

    private void TryContactDamage(Collision collision)
    {
        if (health.IsDead || Time.time < nextContactTime)
            return;

        CombatHealth touchingHealth =
            collision.collider.GetComponentInParent<CombatHealth>();

        if (touchingHealth == null ||
            !touchingHealth.isPlayer ||
            touchingHealth.IsDead)
        {
            return;
        }

        nextContactTime =
            Time.time + Mathf.Max(0.05f, contactInterval);

        touchingHealth.TakeDamage(contactDamage);
    }

    private void OnDisable()
    {
        HideBeam();
        wasAllowedToAttack = false;
    }
}