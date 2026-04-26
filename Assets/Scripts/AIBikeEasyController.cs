using UnityEngine;
using rayzngames;

/// <summary>
/// Option 4 (Vélo Easy Bike en mode IA autonome)
/// Ce script pilote le BicycleVehicle exactement comme un joueur humain,
/// mais de manière totalement autonome :
///   - Il avance toujours (verticalInput = 1)
///   - Il vire en calculant un angle vers une cible (horizontalInput)
///   - Il freine si un obstacle est trop proche (verticalInput < 0)
///   - Il désactive BikeControlsExample pour éviter le conflit clavier
/// Usage : attachez ce script sur le même objet que BicycleVehicle.
/// </summary>
[RequireComponent(typeof(BicycleVehicle))]
public class AIBikeEasyController : MonoBehaviour
{
    [Header("IA Navigation")]
    [Tooltip("Objet ou point que le vélo IA doit rejoindre/suivre")]
    public Transform target;
    [Tooltip("Angle de correction de direction maximum")]
    public float steeringStrength = 0.7f;

    [Header("Détection Obstacles")]
    public float detectionDistance = 12f;
    public float sphereRadius      = 1.5f;

    [Header("Conduite")]
    public float normalThrottle    = 0.7f;   // Pédale normale (0–1)
    public float brakeThrottle     = -0.5f;  // Freinage
    public float emergencyBrake    = -1f;    // Frein maximum

    // ── Privé ───────────────────────────────────────────────────
    private BicycleVehicle bike;
    private BikeControlsExample manualControls;
    private Color gizmoColor = Color.green;

    // ══════════════════════════════════════════════════════════════
    void Start()
    {
        bike = GetComponent<BicycleVehicle>();

        // Désactiver le contrôle clavier pour laisser l'IA prendre la main
        manualControls = GetComponent<BikeControlsExample>();
        if (manualControls != null)
        {
            manualControls.controllingBike = false;
            manualControls.enabled = false;
        }

        bike.InControl(true);
    }

    void Update()
    {
        float throttle  = normalThrottle;
        float steering  = 0f;

        // ── Détection obstacle frontal ───────────────────────────
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        gizmoColor = Color.green;

        if (Physics.SphereCast(origin, sphereRadius,
                               transform.forward, out hit, detectionDistance))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                float dist = hit.distance;

                if (dist < 3f)
                {
                    // Arrêt d'urgence
                    throttle   = emergencyBrake;
                    gizmoColor = Color.red;
                }
                else if (dist < 8f)
                {
                    // Freinage + esquive
                    throttle   = brakeThrottle;
                    gizmoColor = Color.red;

                    // Virer du côté libre
                    Vector3 right = transform.right;
                    Vector3 toObs = (hit.point - transform.position).normalized;
                    float dot     = Vector3.Dot(right, toObs);
                    steering      = dot > 0 ? -steeringStrength : steeringStrength;
                }
            }
        }

        // ── Suivi de cible (si définie) ─────────────────────────
        if (gizmoColor == Color.green && target != null)
        {
            Vector3 toTarget  = (target.position - transform.position).normalized;
            float   angle     = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
            steering = Mathf.Clamp(angle / 45f, -1f, 1f) * steeringStrength;
        }

        // ── Appliquer les entrées comme un joueur virtuel ────────
        bike.horizontalInput = steering;
        bike.verticalInput   = throttle;
    }

    // ══════════════════════════════════════════════════════════════
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, sphereRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                       transform.forward * detectionDistance);

        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
