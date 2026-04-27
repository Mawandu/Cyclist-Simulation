using UnityEngine;
using System.Collections.Generic;
using rayzngames;

/// <summary>
/// IA stable pour BicycleVehicle.
/// Démarre immédiatement, avance à vitesse contrôlée, suit la route UTS,
/// et s'arrête uniquement si un collider physique est détecté devant.
/// </summary>
[RequireComponent(typeof(BicycleVehicle))]
[RequireComponent(typeof(Rigidbody))]
public class AIBikeEasyController : MonoBehaviour
{
    [Header("Vitesse")]
    [Tooltip("Vitesse max en m/s — 2.5 = ~9 km/h (cycliste urbain)")]
    public float maxSpeedMs = 2.5f;
    [Tooltip("Force de pédales appliquée")]
    [Range(0f, 1f)] public float throttle = 0.4f;

    [Header("Direction")]
    [Range(0.5f, 2f)] public float steeringStrength = 1f;
    public float waypointReachDistance = 3f;

    [Header("Freinage")]
    [Tooltip("Distance à partir de laquelle le vélo commence à ralentir")]
    public float slowDistance = 10f;
    [Tooltip("Distance d'arrêt complet")]
    public float stopDistance = 3.5f;
    public float detectionRadius = 0.8f;

    // ── Privé ────────────────────────────────────────────────────
    private BicycleVehicle      bike;
    private Rigidbody           rb;
    private List<Vector3>       route = new List<Vector3>();
    private int                 wpIdx = 0;
    private Color               dbgColor = Color.green;

    // ══════════════════════════════════════════════════════════════
    void Start()
    {
        bike = GetComponent<BicycleVehicle>();
        rb   = GetComponent<Rigidbody>();

        // Désactiver contrôle manuel
        var manual = GetComponent<BikeControlsExample>();
        if (manual != null) manual.enabled = false;

        // Charger les waypoints de la route UTS existante
        BcycleGyroPath path = FindAnyObjectByType<BcycleGyroPath>();
        if (path != null && path.pathPoint != null && path.pathPoint.Count > 0)
        {
            route  = path.pathPoint;
            wpIdx  = ClosestWaypoint();
            Debug.Log($"[AIBike] Route OK : {route.Count} points, départ au #{wpIdx}");
        }
        else
            Debug.LogWarning("[AIBike] Aucune route trouvée — mode ligne droite.");
    }

    void Update()
    {
        // Obligatoire chaque frame (copie de BikeControlsExample)
        bike.InControl(true);
        bike.ConstrainRotation(bike.OnGround());

        float obstDist = ObstacleDistance();
        dbgColor = obstDist < slowDistance ? Color.red : Color.green;

        if (obstDist < stopDistance)
        {
            // Arrêt complet
            bike.braking         = true;
            bike.verticalInput   = 0f;
            bike.horizontalInput = 0f;
            return;
        }

        bike.braking = false;

        // Throttle proportionnel : taille si obstacle loin, 0 si trop rapide
        float speed       = rb.linearVelocity.magnitude;
        float distFactor  = Mathf.InverseLerp(stopDistance, slowDistance, obstDist);
        float wantThrottle = speed < maxSpeedMs ? throttle * distFactor : 0f;
        // Toujours au moins un minimum pour démarrer (si pas d'obstacle)
        if (obstDist >= slowDistance) wantThrottle = speed < maxSpeedMs ? throttle : 0f;

        bike.verticalInput   = wantThrottle;
        bike.horizontalInput = RouteSteer();
    }

    // ── Calcule la distance de l'obstacle le plus proche ─────────
    float ObstacleDistance()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;
        if (Physics.SphereCast(origin, detectionRadius, transform.forward, out hit, slowDistance))
            if (!hit.transform.IsChildOf(transform))
                return hit.distance;
        return slowDistance + 1f; // pas d'obstacle
    }

    // ── Calcule l'angle de virage vers le prochain waypoint ──────
    float RouteSteer()
    {
        if (route.Count == 0) return 0f;

        // Avancer si assez proche du waypoint courant
        if (FlatDist(transform.position, route[wpIdx]) < waypointReachDistance)
            wpIdx = (wpIdx + 1) % route.Count;

        Vector3 toWp  = (route[wpIdx] - transform.position).normalized;
        float   angle = Vector3.SignedAngle(transform.forward, toWp, Vector3.up);
        return Mathf.Clamp(angle / 45f, -1f, 1f) * steeringStrength;
    }

    // ── Trouve le waypoint le plus proche ────────────────────────
    int ClosestWaypoint()
    {
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < route.Count; i++)
        {
            float d = FlatDist(transform.position, route[i]);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    float FlatDist(Vector3 a, Vector3 b)
        => Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));

    void OnDrawGizmos()
    {
        Gizmos.color = dbgColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, detectionRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * slowDistance);
    }
}
