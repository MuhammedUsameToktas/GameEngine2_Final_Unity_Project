using UnityEngine;

/// <summary>
/// Lost soul pickup - dropped when player dies (Elden Ring style)
/// Player can recover souls by reaching this pickup
/// If player dies again before recovering, the pickup is destroyed
/// </summary>
[RequireComponent(typeof(Collider))]
public class LostSoulPickup : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float rotationSpeed = 90f;

    private int storedSouls;
    private int storedCoins;
    private Vector3 startPosition;
    private bool isCollected = false;
    private bool startPositionSet = false;

    /// <summary>
    /// Get stored soul amount (for save system)
    /// </summary>
    public int StoredSouls => storedSouls;

    /// <summary>
    /// Get stored coin amount (for save system)
    /// </summary>
    public int StoredCoins => storedCoins;

    /// <summary>
    /// Get stored soul amount (for backward compatibility with save system)
    /// </summary>
    public int StoredAmount => storedSouls;

    private void Start()
    {
        // Only set startPosition if not already set (from Initialize)
        if (!startPositionSet)
        {
            startPosition = transform.position;
            startPositionSet = true;
        }
        
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"LostSoulPickup on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // Floating animation
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Rotation animation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Initialize the pickup with soul amount (backward compatibility)
    /// </summary>
    public void Initialize(int amount)
    {
        Initialize(amount, 0);
    }

    /// <summary>
    /// Initialize the pickup with soul and coin amounts
    /// </summary>
    public void Initialize(int souls, int coins)
    {
        storedSouls = souls;
        storedCoins = coins;
        startPosition = transform.position; // Set start position from current transform
        startPositionSet = true; // Mark as set so Start() doesn't overwrite it
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (!other.CompareTag("Player")) return;

        PlayerSoul playerSoul = other.GetComponent<PlayerSoul>();
        PlayerCurrency playerCurrency = other.GetComponent<PlayerCurrency>();
        
        bool restored = false;

        // Restore souls to player
        if (playerSoul != null && storedSouls > 0)
        {
            playerSoul.AddSouls(storedSouls);
            restored = true;
        }

        // Restore coins to player
        if (playerCurrency != null && storedCoins > 0)
        {
            playerCurrency.AddCoins(storedCoins);
            restored = true;
        }

        // Only destroy if we actually restored something (or if both are 0, still destroy empty pickup)
        if (restored || (storedSouls == 0 && storedCoins == 0))
        {
            // TODO: Add pickup VFX + sound
            
            // Destroy pickup
            isCollected = true;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when player dies again - destroy this pickup (souls are lost)
    /// </summary>
    public void OnPlayerDeath()
    {
        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }
}
