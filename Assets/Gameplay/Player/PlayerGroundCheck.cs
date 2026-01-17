using UnityEngine;

/// <summary>
/// Reliable ground detection system for platformer gameplay
/// Uses sphere cast to detect ground and provides ground normal for slope handling
/// Exposes IsGrounded, JustLanded, and GroundNormal properties
/// </summary>
public class PlayerGroundCheck : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float checkRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private float raycastOriginOffset = 0.1f;

    public bool IsGrounded { get; private set; }
    public bool JustLanded { get; private set; }
    public Vector3 GroundNormal { get; private set; }

    private bool wasGrounded;

    private void Awake()
    {
        // Auto-create ground check point if not assigned
        if (groundCheckPoint == null)
        {
            GameObject checkPointObj = new GameObject("GroundCheck");
            checkPointObj.transform.SetParent(transform);
            checkPointObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            groundCheckPoint = checkPointObj.transform;
            Debug.LogWarning("PlayerGroundCheck: Auto-created GroundCheck point. Please assign it manually in the Inspector for better control.");
        }

        // Default to "Default" layer if groundLayer is not set
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("PlayerGroundCheck: Ground layer not set. Using 'Default' layer. Please configure in Inspector.");
        }

        GroundNormal = Vector3.up;
    }

    private void Update()
    {
        wasGrounded = IsGrounded;
        IsGrounded = false;
        GroundNormal = Vector3.up;

        // First check: OverlapSphere to quickly detect nearby ground
        Collider[] hits = Physics.OverlapSphere(
            groundCheckPoint.position,
            checkRadius,
            groundLayer
        );

        if (hits.Length > 0)
        {
            // Second check: Raycast to get accurate ground normal
            Vector3 rayOrigin = transform.position + Vector3.up * raycastOriginOffset;
            RaycastHit hit;
            
            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out hit,
                raycastDistance,
                groundLayer))
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
            }
        }

        // Detect landing event
        JustLanded = !wasGrounded && IsGrounded;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null)
            return;

        // Draw sphere check
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, checkRadius);

        // Draw raycast
        Vector3 rayOrigin = transform.position + Vector3.up * raycastOriginOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);

        // Draw ground normal
        if (IsGrounded)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(groundCheckPoint.position, GroundNormal * 0.5f);
        }
    }
}
