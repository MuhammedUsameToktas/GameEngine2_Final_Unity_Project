using UnityEngine;

/// <summary>
/// Controls player animations based on movement state
/// Handles idle/walk/run blending and random idle variants
/// </summary>
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Idle Variants")]
    [SerializeField] private float minIdleVariantInterval = 4f;
    [SerializeField] private float maxIdleVariantInterval = 8f;

    private PlayerMovement movement;
    private float idleTimer;

    // Animator parameter names
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IdleVariantHash = Animator.StringToHash("IdleVariant");

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();

        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator == null)
        {
            Debug.LogWarning("PlayerAnimatorController: No Animator found. Animation will not work.");
        }
    }

    private void Start()
    {
        // Initialize idle timer with random value
        idleTimer = Random.Range(minIdleVariantInterval, maxIdleVariantInterval);
    }

    private void Update()
    {
        if (animator == null || movement == null)
            return;

        float speed01 = movement.GetNormalizedSpeed();

        animator.SetFloat(SpeedHash, speed01);
        animator.SetBool(IsMovingHash, movement.IsMoving());

        HandleIdleVariants();
    }

    private void HandleIdleVariants()
    {
        if (movement.IsMoving())
        {
            // Reset timer when moving
            idleTimer = Random.Range(minIdleVariantInterval, maxIdleVariantInterval);
            return;
        }

        // Count down when idle
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            animator.SetTrigger(IdleVariantHash);
            idleTimer = Random.Range(minIdleVariantInterval, maxIdleVariantInterval);
        }
    }
}

