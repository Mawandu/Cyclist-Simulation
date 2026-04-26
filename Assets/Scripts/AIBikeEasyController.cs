using UnityEngine;
using rayzngames;

/// <summary>
/// IA simple et stable pour BicycleVehicle (Easy Bike System).
/// Stratégie : forcer une vitesse minimale pour éviter que le vélo tombe
/// (le BicycleVehicle perd l'équilibre si rb.velocity < 2 m/s).
/// Esquive seulement si un obstacle est détecté devant.
/// </summary>
[RequireComponent(typeof(BicycleVehicle))]
[RequireComponent(typeof(Rigidbody))]
public class AIBikeEasyController : MonoBehaviour
{
    [Header("Conduite")]
    [Range(0.1f, 1f)]
    public float throttle = 1f;

    [Header("Détection Obstacles")]
    public float detectionDistance = 10f;
    public float sphereRadius = 1.2f;

    [Header("Esquive")]
    [Range(0.1f, 1f)]
    public float dodgeStrength = 0.4f;

    private BicycleVehicle      bike;
    private BikeControlsExample manual;
    private Rigidbody           rb;
    private Color               gizmoColor = Color.green;

    // ══════════════════════════════════════════════════════════════
    void Start()
    {
        bike   = GetComponent<BicycleVehicle>();
        manual = GetComponent<BikeControlsExample>();
        rb     = GetComponent<Rigidbody>();

        // Couper contrôle manuel
        if (manual != null)
        {
            manual.controllingBike = false;
            manual.enabled = false;
        }

        bike.InControl(true);

        // IMPORTANT : bloquer la rotation Z pour stabiliser le vélo
        // (sinon il chute avant d'atteindre la vitesse d'équilibre)
        bike.ConstrainRotation(true);

        // Donner une impulsion initiale pour dépasser les 2 m/s de suite
        rb.AddForce(transform.forward * 300f, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // Débloquer la rotation Z une fois qu'on a assez de vitesse
        if (rb.linearVelocity.magnitude > 2.5f)
            bike.ConstrainRotation(false);

        float steer = ComputeSteering();

        bike.braking          = false;
        bike.verticalInput    = throttle;
        bike.horizontalInput  = steer;
    }

    float ComputeSteering()
    {
        gizmoColor = Color.green;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (!Physics.SphereCast(origin, sphereRadius,
                                transform.forward, out hit, detectionDistance))
            return 0f;

        if (hit.transform.IsChildOf(transform))
            return 0f;

        float dist = hit.distance;
        gizmoColor = Color.red;

        if (dist < 3f)
        {
            // Freinage d'urgence via la propriété braking
            bike.braking         = true;
            bike.verticalInput   = 0f;
            return 0f;
        }

        // Calculer le côté d'esquive
        Vector3 right = transform.right;
        Vector3 toObs = (hit.point - transform.position).normalized;
        float   dot   = Vector3.Dot(right, toObs);
        return dot > 0 ? -dodgeStrength : dodgeStrength;
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
