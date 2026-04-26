using UnityEngine;
using rayzngames;

/// <summary>
/// IA simple et stable pour BicycleVehicle (Easy Bike System).
/// Stratégie : avancer tout droit (verticalInput = 1 en permanence).
/// Esquive seulement si un obstacle est détecté devant.
/// Désactive BikeControlsExample pour éviter les conflits.
/// </summary>
[RequireComponent(typeof(BicycleVehicle))]
public class AIBikeEasyController : MonoBehaviour
{
    [Header("Conduite")]
    [Range(0.1f, 1f)]
    public float throttle = 0.8f;          // Pédale (avant = positif)

    [Header("Détection Obstacles")]
    public float detectionDistance = 10f;
    public float sphereRadius      = 1.2f;

    [Header("Esquive")]
    [Range(0.1f, 1f)]
    public float dodgeStrength     = 0.5f;  // Force de virage lors de l'esquive

    private BicycleVehicle      bike;
    private BikeControlsExample manual;
    private Color               gizmoColor = Color.green;
    private float               steer      = 0f;

    // ══════════════════════════════════════════════════════════════
    void Start()
    {
        bike   = GetComponent<BicycleVehicle>();
        manual = GetComponent<BikeControlsExample>();

        // Couper le contrôle manuel proprement
        if (manual != null)
        {
            manual.controllingBike = false;
            manual.enabled         = false;
        }

        bike.InControl(true);
    }

    void FixedUpdate()
    {
        steer = 0f;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        gizmoColor = Color.green;

        if (Physics.SphereCast(origin, sphereRadius,
                               transform.forward, out hit, detectionDistance))
        {
            // Ignore ses propres parties
            if (!hit.transform.IsChildOf(transform))
            {
                float dist = hit.distance;
                gizmoColor = Color.red;

                if (dist < 3f)
                {
                    // Arrêt total
                    bike.verticalInput   = -1f;
                    bike.horizontalInput = 0f;
                    return;
                }

                // Calculer de quel côté l'obstacle se trouve
                Vector3 right = transform.right;
                Vector3 toObs = (hit.point - transform.position).normalized;
                float   dot   = Vector3.Dot(right, toObs);

                // Esquiver du côté opposé
                steer = dot > 0 ? -dodgeStrength : dodgeStrength;
            }
        }

        // Appliquer : toujours avancer, parfois virer
        bike.verticalInput   = throttle;
        bike.horizontalInput = steer;
    }

    // ══════════════════════════════════════════════════════════════
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, sphereRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                       transform.forward * detectionDistance);
    }
}
