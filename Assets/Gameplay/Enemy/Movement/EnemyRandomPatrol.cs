using UnityEngine;

/// <summary>
/// Enemy that randomly patrols within a defined area
/// Chooses random waypoints and moves between them
/// </summary>
public class EnemyRandomPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float patrolRadius = 5f; // Radius from start position
    [SerializeField] private float waypointReachDistance = 0.5f; // Distance to consider waypoint reached
    [SerializeField] private float minWaitTime = 1f; // Min time to wait at waypoint
    [SerializeField] private float maxWaitTime = 3f; // Max time to wait at waypoint
    [SerializeField] private float rotationSpeed = 5f;

    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float waitDuration = 0f;

    private void Start()
    {
        startPosition = transform.position;
        SetNewWaypoint();
    }

    private void Update()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                isWaiting = false;
                SetNewWaypoint();
            }
        }
        else
        {
            MoveToWaypoint();
        }
    }

    private void SetNewWaypoint()
    {
        // Generate random waypoint within patrol radius
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Ensure waypoint is on ground (raycast down)
        RaycastHit hit;
        if (Physics.Raycast(currentWaypoint + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            currentWaypoint.y = hit.point.y;
        }
    }

    private void MoveToWaypoint()
    {
        Vector3 direction = (currentWaypoint - transform.position);
        float distance = direction.magnitude;

        if (distance <= waypointReachDistance)
        {
            // Reached waypoint, wait
            isWaiting = true;
            waitTimer = 0f;
            waitDuration = Random.Range(minWaitTime, maxWaitTime);
            return;
        }

        // Move towards waypoint
        direction.y = 0; // Keep movement horizontal
        direction.Normalize();

        transform.position += direction * speed * Time.deltaTime;

        // Rotate towards waypoint
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw patrol area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius);

        // Draw current waypoint
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}
