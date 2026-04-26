using UnityEngine;
using System.Collections;

/// <summary>
/// Option 4 — Cycliste IA Totalement Autonome
/// Stratégie : UTS_MovePath gère le déplacement de base.
/// Ce script INTERCEPTE uniquement pour :
///   - Freinage dans les virages
///   - Esquive d'obstacles
///   - Arrêt d'urgence + redémarrage progressif
/// Il ne démarre PAS le vélo lui-même (UTS le fait).
/// </summary>
public class AIBikeFullAutonomous : MonoBehaviour
{
    [Header("Détection Obstacles")]
    public float detectionDistance  = 15f;
    public float sphereRadius       = 2f;

    [Header("Vitesses")]
    [Range(0.1f, 1f)] public float curveSpeedFactor    = 0.6f;
    [Range(0.1f, 1f)] public float obstacleSlowFactor  = 0.4f;

    [Header("Esquive")]
    public float maxDodgeOffset     = 2.5f;
    public float dodgeSpeed         = 5f;

    [Header("Redémarrage")]
    public float restartDelay       = 1.0f;
    public float restartAccelTime   = 2.0f;

    // ── Privé ─────────────────────────────────────────────────────
    private BcycleGyroController ctrl;
    private float                originalSpeed;
    private Vector3              dodgeOffset;
    private bool                 isRestarting;
    private Color                gizmoColor = Color.green;

    // ══════════════════════════════════════════════════════════════
    void Start()
    {
        ctrl          = GetComponent<BcycleGyroController>();
        originalSpeed = ctrl.moveSpeed;
        // Ne pas toucher à tempStop ici — UTS démarre seul
    }

    void Update()
    {
        if (isRestarting) return;

        // ── Raycast obstacle frontal ─────────────────────────────
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f
                       + transform.forward * 1.5f;

        bool obstacleAhead = false;
        float obstDist     = detectionDistance;

        if (Physics.SphereCast(origin, sphereRadius, transform.forward,
                               out hit, detectionDistance))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                bool tagged    = hit.transform.CompareTag("Car")
                              || hit.transform.CompareTag("Bcycle")
                              || hit.transform.CompareTag("Player")
                              || hit.transform.CompareTag("Obstacle");
                bool hasRigid  = hit.transform.GetComponentInParent<Rigidbody>() != null;

                if (tagged || hasRigid)
                {
                    obstacleAhead = true;
                    obstDist      = hit.distance;
                }
            }
        }

        // ── Arrêt d'urgence ─────────────────────────────────────
        if (obstacleAhead && obstDist < 4f)
        {
            if (!ctrl.tempStop)
            {
                ctrl.tempStop  = true;
                ctrl.moveSpeed = 0f;
                var rb = GetComponent<Rigidbody>();
                if (rb) rb.linearVelocity = Vector3.zero;
                StartCoroutine(RestartRoutine());
            }
            gizmoColor = Color.red;
            return;
        }

        ctrl.tempStop = false;

        // ── Obstacle loin → ralentir + esquiver ─────────────────
        if (obstacleAhead)
        {
            gizmoColor = Color.red;

            Vector3 right    = transform.right;
            Vector3 toObs    = (hit.point - transform.position).normalized;
            float   dot      = Vector3.Dot(right, toObs);
            float   dodgeDir = dot > 0 ? -1f : 1f;

            dodgeOffset = Vector3.Lerp(dodgeOffset,
                              right * dodgeDir * maxDodgeOffset,
                              Time.deltaTime * dodgeSpeed);
            dodgeOffset = Vector3.ClampMagnitude(dodgeOffset, maxDodgeOffset);

            ctrl.moveSpeed = Mathf.Lerp(ctrl.moveSpeed,
                                 originalSpeed * obstacleSlowFactor,
                                 Time.deltaTime * 3f);
        }
        else
        {
            // ── Virage : angle entre forward et prochain cap ─────
            float turnAngle = Vector3.Angle(transform.forward,
                                 transform.parent != null
                                     ? transform.parent.forward
                                     : transform.forward);

            if (turnAngle > 30f)
            {
                gizmoColor     = Color.yellow;
                ctrl.moveSpeed = Mathf.Lerp(ctrl.moveSpeed,
                                     originalSpeed * curveSpeedFactor,
                                     Time.deltaTime * 2f);
            }
            else
            {
                gizmoColor     = Color.green;
                ctrl.moveSpeed = Mathf.Lerp(ctrl.moveSpeed,
                                     originalSpeed,
                                     Time.deltaTime * 2f);
            }

            // Revenir sur la trajectoire d'origine
            dodgeOffset = Vector3.Lerp(dodgeOffset,
                              Vector3.zero, Time.deltaTime * dodgeSpeed);
        }
    }

    // ══════════════════════════════════════════════════════════════
    IEnumerator RestartRoutine()
    {
        isRestarting = true;
        gizmoColor   = Color.yellow;

        yield return new WaitForSeconds(restartDelay);

        ctrl.tempStop = false;
        float elapsed = 0f;

        while (elapsed < restartAccelTime)
        {
            elapsed += Time.deltaTime;
            ctrl.moveSpeed = Mathf.Lerp(0f, originalSpeed,
                                        elapsed / restartAccelTime);
            yield return null;
        }

        ctrl.moveSpeed = originalSpeed;
        isRestarting   = false;
        gizmoColor     = Color.green;
    }

    // ══════════════════════════════════════════════════════════════
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 origin = transform.position + Vector3.up * 0.5f
                       + transform.forward * 1.5f;
        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.DrawRay(origin, transform.forward * detectionDistance);
    }
}
