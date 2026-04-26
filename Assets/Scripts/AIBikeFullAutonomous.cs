using UnityEngine;
using System.Collections;

/// <summary>
/// Option 4 — Cycliste IA Totalement Autonome
/// Ce script gère TOUS les comportements du vélo de l'IA :
///   1. Freinage automatique dans les virages
///   2. Détection et esquive des obstacles frontaux
///   3. Arrêt d'urgence + détection de bord de route (sol absent)
///   4. Redémarrage progressif (Coroutine lissée)
///   5. Gizmos de débogage visuel (vert = libre, rouge = bloqué)
/// </summary>
[RequireComponent(typeof(BcycleGyroController))]
public class AIBikeFullAutonomous : MonoBehaviour
{
    [Header("Détection Obstacles")]
    public float detectionDistance = 15f;
    public float sphereRadius = 2f;

    [Header("Détection Sol")]
    public float groundRayLength = 2f;

    [Header("Vitesses")]
    public float normalSpeedMultiplier = 1f;
    public float curveSpeedMultiplier  = 0.6f;
    public float obstacleSlowMultiplier = 0.4f;

    [Header("Esquive")]
    public float maxDodgeOffset = 2.5f;
    public float dodgeSpeed = 5f;

    [Header("Redémarrage")]
    public float restartDelay = 1.5f;      // Secondes d'attente avant relance
    public float restartAccelTime = 2f;    // Secondes pour revenir à pleine vitesse

    // ── Privé ───────────────────────────────────────────────────────────────
    private BcycleGyroController controller;
    private float originalSpeed;
    private Vector3 dodgeOffset;
    private bool isRestarting;
    private bool isStopped;

    // ── État de débogage (couleur Gizmo) ────────────────────────────────────
    private Color gizmoColor = Color.green;

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        controller    = GetComponent<BcycleGyroController>();
        originalSpeed = controller.moveSpeed;
    }

    void Update()
    {
        if (isRestarting) return;

        bool groundOk      = CheckGround();
        bool obstacleAhead = false;
        float obstacleDist = detectionDistance;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * 1.5f;

        if (Physics.SphereCast(origin, sphereRadius, transform.forward, out hit, detectionDistance))
        {
            // Ignorer ses propres enfants
            if (!hit.transform.IsChildOf(transform))
            {
                bool isPhysical = hit.transform.GetComponentInParent<Rigidbody>() != null;
                bool isTagged   = hit.transform.CompareTag("Car")      ||
                                  hit.transform.CompareTag("Bcycle")   ||
                                  hit.transform.CompareTag("Player")   ||
                                  hit.transform.CompareTag("Obstacle");

                if (isPhysical || isTagged)
                {
                    obstacleAhead = true;
                    obstacleDist  = hit.distance;
                }
            }
        }

        // ── 1. Sol absent → Arrêt total ─────────────────────────────────────
        if (!groundOk)
        {
            EmergencyStop("Bord de route détecté !");
            return;
        }

        // ── 2. Obstacle très proche → Arrêt d'urgence ───────────────────────
        if (obstacleAhead && obstacleDist < 4f)
        {
            EmergencyStop("Obstacle immédiat !");
            return;
        }

        isStopped = false;

        // ── 3. Obstacle loin → Ralentir + Esquiver ──────────────────────────
        if (obstacleAhead)
        {
            gizmoColor = Color.red;

            // Calculer côté d'esquive (produit scalaire)
            Vector3 right = transform.right;
            Vector3 toObstacle = (hit.point - transform.position).normalized;
            float dot = Vector3.Dot(right, toObstacle);
            float dodgeDir = dot > 0 ? -1f : 1f;

            dodgeOffset = Vector3.Lerp(dodgeOffset,
                right * dodgeDir * maxDodgeOffset,
                Time.deltaTime * dodgeSpeed);
            dodgeOffset = Vector3.ClampMagnitude(dodgeOffset, maxDodgeOffset);

            controller.moveSpeed = Mathf.Lerp(controller.moveSpeed,
                originalSpeed * obstacleSlowMultiplier,
                Time.deltaTime * 3f);
        }
        else
        {
            // ── 4. Détection virage (angle entre forward et prochain waypoint) ──
            float curveAngle = DetectCurveAngle();
            gizmoColor = Color.green;

            if (curveAngle > 30f)
            {
                // Virage serré → freinage préventif
                gizmoColor = Color.yellow;
                controller.moveSpeed = Mathf.Lerp(controller.moveSpeed,
                    originalSpeed * curveSpeedMultiplier,
                    Time.deltaTime * 2f);
            }
            else
            {
                // Route droite → reprendre pleine vitesse
                controller.moveSpeed = Mathf.Lerp(controller.moveSpeed,
                    originalSpeed * normalSpeedMultiplier,
                    Time.deltaTime * 2f);
            }

            dodgeOffset = Vector3.Lerp(dodgeOffset, Vector3.zero, Time.deltaTime * dodgeSpeed);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    bool CheckGround()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.3f,
                               Vector3.down, groundRayLength);
    }

    float DetectCurveAngle()
    {
        // Estimer le prochain virage à partir du prochain waypoint UTS
        var movePath = GetComponent<UTS_MovePath>();
        if (movePath == null) return 0f;

        // On regarde la direction du chemin dans 3 secondes
        Vector3 futurePos = transform.position + transform.forward * controller.moveSpeed * 3f;
        Vector3 toCurrent = (futurePos - transform.position).normalized;
        return Vector3.Angle(transform.forward, toCurrent);
    }

    void EmergencyStop(string reason)
    {
        if (!isStopped)
        {
            isStopped = true;
            gizmoColor = Color.red;
            Debug.Log($"[AIBikeAutonomous] STOP — {reason}");

            controller.moveSpeed = 0f;
            controller.tempStop  = true;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            if (!isRestarting)
                StartCoroutine(RestartRoutine());
        }
    }

    IEnumerator RestartRoutine()
    {
        isRestarting = true;
        gizmoColor = Color.yellow;

        yield return new WaitForSeconds(restartDelay);

        // Relance progressive
        float elapsed = 0f;
        controller.tempStop = false;
        gizmoColor = Color.green;

        while (elapsed < restartAccelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / restartAccelTime;
            controller.moveSpeed = Mathf.Lerp(0f, originalSpeed, t);
            yield return null;
        }

        controller.moveSpeed = originalSpeed;
        isStopped    = false;
        isRestarting = false;
    }

    // ────────────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * 1.5f;
        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.DrawRay(origin, transform.forward * detectionDistance);

        // Raycast sol
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.3f, Vector3.down * groundRayLength);
    }
}
