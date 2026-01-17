using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// CinemachineVirtualDynamic - Handles dynamic assignment of Cinemachine camera targets
/// Attach this to a CinemachineFreeLook or CinemachineVirtualCamera GameObject
/// LevelManager calls AssignTarget() after spawning the player
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCameraBase))]
public class CinemachineVirtualDynamic : MonoBehaviour
{
    [Header("Camera Target")]
    [SerializeField] private string cameraTargetName = "CameraTarget";
    [SerializeField] private bool usePlayerRootIfTargetNotFound = true;

    private CinemachineVirtualCameraBase virtualCamera;
    private CinemachineFreeLook freeLookCamera;
    private CinemachineVirtualCamera standardVirtualCamera;

    private void Awake()
    {
        // Get the appropriate Cinemachine component
        virtualCamera = GetComponent<CinemachineVirtualCameraBase>();
        freeLookCamera = GetComponent<CinemachineFreeLook>();
        standardVirtualCamera = GetComponent<CinemachineVirtualCamera>();

        // Ensure CinemachineBrain exists on main camera
        SetupCinemachineBrain();
    }

    /// <summary>
    /// Ensure CinemachineBrain exists on the main camera
    /// </summary>
    private void SetupCinemachineBrain()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main camera not found. CinemachineBrain setup skipped.");
            return;
        }

        if (!Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain))
        {
            brain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
            // Configure default blend time (1 second)
            var defaultBlend = brain.DefaultBlend;
            defaultBlend.Time = 1f;
            brain.DefaultBlend = defaultBlend;
            brain.ShowDebugText = false;
            Debug.Log("CinemachineBrain added to main camera.");
        }
    }

    /// <summary>
    /// Assign camera target from player GameObject
    /// Called by LevelManager after spawning the player
    /// </summary>
    /// <param name="player">The player GameObject instance</param>
    public void AssignTarget(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("Cannot assign camera target: Player GameObject is null");
            return;
        }

        Transform targetTransform = FindCameraTarget(player.transform);

        if (targetTransform == null)
        {
            Debug.LogError($"Cannot assign camera target: CameraTarget '{cameraTargetName}' not found on Player prefab!");
            return;
        }

        AssignTargetTransform(targetTransform);
    }

    /// <summary>
    /// Assign camera target directly from Transform
    /// </summary>
    /// <param name="target">The target Transform to follow/look at</param>
    public void AssignTargetTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogError("Cannot assign camera target: Transform is null");
            return;
        }

        if (freeLookCamera != null)
        {
            // CinemachineFreeLook has separate Follow and LookAt
            freeLookCamera.Follow = target;
            freeLookCamera.LookAt = target;
            Debug.Log($"CinemachineFreeLook assigned to target: {target.name}");
        }
        else if (standardVirtualCamera != null)
        {
            // CinemachineVirtualCamera has separate Follow and LookAt
            standardVirtualCamera.Follow = target;
            standardVirtualCamera.LookAt = target;
            Debug.Log($"CinemachineVirtualCamera assigned to target: {target.name}");
        }
        else if (virtualCamera != null)
        {
            // Generic fallback for other CinemachineVirtualCameraBase types
            // Try to set Follow and LookAt via reflection if available
            var followProperty = virtualCamera.GetType().GetProperty("Follow");
            var lookAtProperty = virtualCamera.GetType().GetProperty("LookAt");

            if (followProperty != null)
            {
                followProperty.SetValue(virtualCamera, target);
            }

            if (lookAtProperty != null)
            {
                lookAtProperty.SetValue(virtualCamera, target);
            }

            Debug.Log($"CinemachineVirtualCameraBase assigned to target: {target.name}");
        }
        else
        {
            Debug.LogError("No valid Cinemachine camera component found!");
        }
    }

    /// <summary>
    /// Find the CameraTarget transform in the player hierarchy
    /// </summary>
    private Transform FindCameraTarget(Transform playerRoot)
    {
        // First, try to find by name
        Transform target = playerRoot.Find(cameraTargetName);

        if (target != null)
        {
            return target;
        }

        // If not found and fallback is enabled, use player root
        if (usePlayerRootIfTargetNotFound)
        {
            Debug.LogWarning($"CameraTarget '{cameraTargetName}' not found, using player root transform instead.");
            return playerRoot;
        }

        return null;
    }

    /// <summary>
    /// Get the current camera component
    /// </summary>
    public CinemachineVirtualCameraBase GetCamera()
    {
        return virtualCamera;
    }
}

