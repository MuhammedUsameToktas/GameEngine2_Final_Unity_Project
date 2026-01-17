using UnityEngine;

/// <summary>
/// Marks a point in the world that the player can swing from
/// Place this component on GameObjects where you want swing points
/// </summary>
public class SwingPoint : MonoBehaviour
{
    [Header("Swing Point Settings")]
    [Tooltip("Maximum distance the player can be to attach to this swing point")]
    [SerializeField] private float maxSwingDistance = 15f;
    
    [Tooltip("Visual indicator for the swing point (optional)")]
    [SerializeField] private GameObject visualIndicator;
    
    [Tooltip("Layer mask for what can block the swing line (walls, obstacles, etc.)")]
    [SerializeField] private LayerMask obstacleLayer = -1;
    
    public float MaxSwingDistance => maxSwingDistance;
    public LayerMask ObstacleLayer => obstacleLayer;
    public Vector3 Position => transform.position;
    
    private void Awake()
    {
        // Create a simple visual indicator if none is assigned
        if (visualIndicator == null)
        {
            CreateDefaultVisual();
        }
    }
    
    private void CreateDefaultVisual()
    {
        // Create a small sphere as visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "SwingPointIndicator";
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = Vector3.one * 0.5f;
        
        // Remove collider (we don't need it for the indicator)
        Collider col = indicator.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }
        
        // Make it semi-transparent
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange, semi-transparent
            renderer.material = mat;
        }
        
        visualIndicator = indicator;
    }
    
    /// <summary>
    /// Check if the player can swing from this point (within range and line of sight)
    /// </summary>
    public bool CanSwingFrom(Vector3 playerPosition)
    {
        float distance = Vector3.Distance(playerPosition, Position);
        
        // Check if within range
        if (distance > maxSwingDistance)
            return false;
        
        // Check line of sight (no obstacles blocking)
        Vector3 direction = (Position - playerPosition).normalized;
        float distanceToPoint = Vector3.Distance(playerPosition, Position);
        
        RaycastHit hit;
        if (Physics.Raycast(playerPosition, direction, out hit, distanceToPoint, obstacleLayer))
        {
            // Something is blocking the path
            return false;
        }
        
        return true;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw range sphere
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxSwingDistance);
        
        // Draw point
        Gizmos.color = Color.orange;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
}
