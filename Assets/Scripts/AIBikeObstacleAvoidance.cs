using UnityEngine;

[RequireComponent(typeof(BcycleGyroController))]
[RequireComponent(typeof(UTS_MovePath))]
public class AIBikeObstacleAvoidance : MonoBehaviour
{
    private BcycleGyroController controller;
    private UTS_MovePath movePath;

    [Header("Obstacle Avoidance Triggers")]
    public float detectionDistance = 15f;
    public float dodgeForce = 8f; // Augmenté pour forcer une déviation rapide
    public float resumeSpeed = 2f;
    public float slowDownFactor = 0.5f;

    private Vector3 currentAvoidanceOffset = Vector3.zero;
    private float originalMoveSpeed;

    void Start()
    {
        controller = GetComponent<BcycleGyroController>();
        movePath = GetComponent<UTS_MovePath>();
        originalMoveSpeed = controller.moveSpeed;
    }

    void Update()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f + transform.forward * 1.5f;

        bool avoiding = false;

        // Trigger 1: Proximity to generic tags (rayon étendu pour détecter plus large)
        if (Physics.SphereCast(rayStart, 2.0f, transform.forward, out hit, detectionDistance))
        {
            // Ignorer son propre collider ou ses enfants
            if (hit.transform.IsChildOf(transform) || hit.transform.gameObject == gameObject)
                return;

            // On vérifie si c'est un obstacle (soit par Tag, soit s'il a un Rigidbody comme une voiture non-taggée, ou s'il fait partie du trafic UTS)
            bool isObstacle = hit.transform.CompareTag("Car") || 
                              hit.transform.CompareTag("Bcycle") || 
                              hit.transform.CompareTag("Player") || 
                              hit.transform.CompareTag("Obstacle") ||
                              hit.transform.GetComponentInParent<Rigidbody>() != null ||
                              hit.transform.name.ToLower().Contains("car") ||
                              hit.transform.name.ToLower().Contains("people");

            if (isObstacle)
            {
                float distance = Vector3.Distance(transform.position, hit.point);

                // Action 1: Deviate trajectory FORTEMENT
                Vector3 right = transform.right;
                Vector3 directionToHit = (hit.point - transform.position).normalized;
                float dot = Vector3.Dot(right, directionToHit);
                
                // Esquive opposée au côté touché
                float dodgeDirection = dot > 0 ? -1f : 1f;

                currentAvoidanceOffset = Vector3.Lerp(currentAvoidanceOffset, right * dodgeDirection * dodgeForce, Time.deltaTime * 5f);
                
                // Limite stricte pour empêcher le vélo de sortir de la route (max 2.5 mètres d'écart)
                currentAvoidanceOffset = Vector3.ClampMagnitude(currentAvoidanceOffset, 2.5f);
                
                avoiding = true;

                // Action 2: Arrêt d'urgence si trop près
                if (distance < 4f)
                {
                    controller.tempStop = true;
                    controller.moveSpeed = 0f;
                    // On empêche les chocs physiques de propulser le vélo au loin
                    if (GetComponent<Rigidbody>() != null) {
                        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                    }
                }
                else
                {
                    // Action 3: Ralentir mais continuer à esquiver
                    controller.moveSpeed = Mathf.Lerp(controller.moveSpeed, originalMoveSpeed * slowDownFactor, Time.deltaTime * 2f);
                    controller.tempStop = false;
                }
            }
        }

        if (!avoiding)
        {
            // Action 4: Resume path and normal speed
            currentAvoidanceOffset = Vector3.Lerp(currentAvoidanceOffset, Vector3.zero, Time.deltaTime * resumeSpeed);
            if (!controller.tempStop)
                controller.moveSpeed = Mathf.Lerp(controller.moveSpeed, originalMoveSpeed, Time.deltaTime * resumeSpeed);
        }

        // Apply dynamically to pathfinding offset (which UTS_FullPack naturally follows)
        movePath.randXFinish = currentAvoidanceOffset.x;
        movePath.randZFinish = currentAvoidanceOffset.z;
    }
}
