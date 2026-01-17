using UnityEngine;
using DG.Tweening;

/// <summary>
/// Handles enemy death, animations, and reward drops
/// Spawns coins with bounce animation when enemy dies
/// </summary>
public class EnemyDeath : MonoBehaviour
{
    [Header("Drops")]
    [SerializeField] private int soulDrop = 10;
    [SerializeField] private int coinDrop = 5;
    [SerializeField] private GameObject coinPrefab; // Coin pickup prefab

    [Header("Death Animation")]
    [SerializeField] private float knockbackDuration = 0.3f; // Time to show knockback before death animation
    [SerializeField] private float deathAnimationDuration = 0.5f;
    [SerializeField] private float deathScale = 0.1f; // Scale down to this before destroying
    [SerializeField] private float deathKnockbackForce = 5f; // Knockback force when dying

    [Header("Stomp Death Animation")]
    [SerializeField] private float stompSquishDuration = 0.3f; // Duration of squish animation for stomp
    [SerializeField] private float stompSquishScaleY = 0.2f; // How much to squish vertically (Y scale)
    [SerializeField] private float stompSquishScaleXZ = 1.3f; // How much to expand horizontally (X and Z scale)

    [Header("Coin Spawn Settings")]
    [SerializeField] private float coinSpawnRadius = 2f;
    [SerializeField] private float coinExplosionForce = 8f; // Force applied to coins
    [SerializeField] private float coinUpwardForce = 6f; // Upward force for coins
    [SerializeField] private float coinRandomSpread = 2f; // Random spread angle

    private bool isDying = false;
    private bool isStompDeath = false;
    private EnemyKnockback knockback;

    private void Awake()
    {
        knockback = GetComponent<EnemyKnockback>();
    }

    /// <summary>
    /// Handle death from normal attack (with knockback)
    /// </summary>
    public void HandleDeath()
    {
        HandleDeath(false);
    }

    /// <summary>
    /// Handle death - can be from stomp (no knockback) or normal attack (with knockback)
    /// </summary>
    public void HandleDeath(bool isStomp)
    {
        if (isDying) return;
        isDying = true;
        isStompDeath = isStomp;

        // Disable enemy behavior components immediately
        EnemyBehavior behavior = GetComponent<EnemyBehavior>();
        EnemyFlyingBehavior flyingBehavior = GetComponent<EnemyFlyingBehavior>();
        if (behavior != null) behavior.enabled = false;
        if (flyingBehavior != null) flyingBehavior.enabled = false;

        // For stomp deaths: skip knockback and go straight to squish animation
        if (isStompDeath)
        {
            PlayStompDeathAnimation();
        }
        else
        {
            // For normal attacks: apply knockback first
            if (knockback != null && !knockback.IsKnockedBack)
            {
                // Get direction away from player
                Vector3 knockbackDir = Vector3.zero;
                var player = FindObjectOfType<PlayerMovement>();
                if (player != null)
                {
                    knockbackDir = (transform.position - player.transform.position).normalized;
                    knockbackDir.y = 0; // Keep horizontal
                    knockbackDir.Normalize();
                }
                else
                {
                    // Random direction if no player found
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    knockbackDir = new Vector3(randomDir.x, 0, randomDir.y);
                }
                
                knockback.ApplyKnockback(knockbackDir, deathKnockbackForce);
            }

            // Wait for knockback animation, then play death animation
            Invoke(nameof(PlayDeathAnimationDelayed), knockbackDuration);
        }
        
        // Spawn coins immediately (they'll bounce with physics)
        SpawnCoins();
        
        // Drop souls (directly to player, no pickup)
        DropSouls();
    }

    private void PlayDeathAnimationDelayed()
    {
        PlayDeathAnimation();
    }

    /// <summary>
    /// Play stomp death animation - squish effect (no knockback)
    /// </summary>
    private void PlayStompDeathAnimation()
    {
        Sequence stompSequence = DOTween.Sequence();

        Vector3 originalScale = transform.localScale;
        
        // Squish animation: compress Y, expand X and Z
        Vector3 squishScale = new Vector3(
            originalScale.x * stompSquishScaleXZ,
            originalScale.y * stompSquishScaleY,
            originalScale.z * stompSquishScaleXZ
        );

        // Squish down
        stompSequence.Append(transform.DOScale(squishScale, stompSquishDuration * 0.5f)
            .SetEase(Ease.OutQuad));

        // Then scale down to nothing
        stompSequence.Append(transform.DOScale(Vector3.zero, stompSquishDuration * 0.5f)
            .SetEase(Ease.InBack));

        // Fade out if renderer supports it
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat != null && mat.HasProperty("_Color"))
            {
                stompSequence.Join(renderer.material.DOFade(0f, stompSquishDuration));
            }
        }

        // Destroy after animation
        stompSequence.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Play normal death animation (with rotation and scale down)
    /// </summary>
    private void PlayDeathAnimation()
    {
        // Create death animation sequence
        Sequence deathSequence = DOTween.Sequence();

        // Scale down and rotate
        deathSequence.Append(transform.DOScale(transform.localScale * deathScale, deathAnimationDuration)
            .SetEase(Ease.InBack));
        
        deathSequence.Join(transform.DORotate(new Vector3(0, 0, 180), deathAnimationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad));

        // Fade out if renderer supports it
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            if (mat != null && mat.HasProperty("_Color"))
            {
                deathSequence.Join(renderer.material.DOFade(0f, deathAnimationDuration));
            }
        }

        // Destroy after animation
        deathSequence.OnComplete(() => Destroy(gameObject));
    }

    private void SpawnCoins()
    {
        if (coinDrop <= 0) return;

        // Try to load coin prefab from Resources if not assigned
        GameObject prefabToUse = coinPrefab;
        if (prefabToUse == null)
        {
            prefabToUse = Resources.Load<GameObject>("Prefabs/Coin");
        }

        if (prefabToUse == null)
        {
            Debug.LogWarning($"EnemyDeath on {gameObject.name}: Coin prefab not found. Coins will not spawn.");
            return;
        }

        // Get direction away from player (or random if no player)
        Vector3 explosionDirection = Vector3.zero;
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            explosionDirection = (transform.position - player.transform.position).normalized;
        }

        // Spawn coins with physics-based explosion
        for (int i = 0; i < coinDrop; i++)
        {
            // Calculate spawn position at enemy location
            Vector3 spawnPosition = transform.position;
            spawnPosition.y += 0.5f; // Slightly above enemy center

            // Instantiate coin
            GameObject coin = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);

            // Ensure coin has both trigger and physics colliders
            Collider[] existingColliders = coin.GetComponents<Collider>();
            Collider triggerCollider = null;
            Collider physicsCollider = null;

            // Find existing colliders
            foreach (Collider col in existingColliders)
            {
                if (col.isTrigger)
                {
                    triggerCollider = col; // Keep trigger collider for pickup detection
                }
                else
                {
                    physicsCollider = col; // Keep non-trigger collider for physics
                }
            }

            // If no trigger collider exists, create one
            if (triggerCollider == null)
            {
                SphereCollider newTrigger = coin.AddComponent<SphereCollider>();
                newTrigger.isTrigger = true;
                // Match size with existing collider if available
                if (existingColliders.Length > 0)
                {
                    SphereCollider existingSphere = existingColliders[0] as SphereCollider;
                    if (existingSphere != null)
                    {
                        newTrigger.radius = existingSphere.radius;
                        newTrigger.center = existingSphere.center;
                    }
                    else
                    {
                        // Default size
                        newTrigger.radius = 0.3f;
                    }
                }
                else
                {
                    // Default size
                    newTrigger.radius = 0.3f;
                }
                triggerCollider = newTrigger;
            }

            // If no physics collider exists, create one
            if (physicsCollider == null)
            {
                SphereCollider newPhysics = coin.AddComponent<SphereCollider>();
                newPhysics.isTrigger = false;
                // Match size with trigger collider if it exists
                if (triggerCollider != null)
                {
                    SphereCollider sphereTrigger = triggerCollider as SphereCollider;
                    if (sphereTrigger != null)
                    {
                        newPhysics.radius = sphereTrigger.radius;
                        newPhysics.center = sphereTrigger.center;
                    }
                    else
                    {
                        // Default size
                        newPhysics.radius = 0.3f;
                    }
                }
                else
                {
                    // Default size
                    newPhysics.radius = 0.3f;
                }
                physicsCollider = newPhysics;
            }

            // Add Rigidbody if coin doesn't have one (for physics bouncing)
            Rigidbody coinRb = coin.GetComponent<Rigidbody>();
            if (coinRb == null)
            {
                coinRb = coin.AddComponent<Rigidbody>();
            }
            
            // Configure rigidbody for bouncing
            coinRb.mass = 0.1f;
            coinRb.linearDamping = 1f;
            coinRb.angularDamping = 2f;
            coinRb.useGravity = true; // Enable gravity for falling
            coinRb.isKinematic = false; // Make sure it's not kinematic
            
            // Add physics material for bouncing if not present
            PhysicsMaterial bounceMaterial = new PhysicsMaterial("CoinBounce");
            bounceMaterial.bounciness = 0.6f; // Bouncy material
            bounceMaterial.staticFriction = 0.4f;
            bounceMaterial.dynamicFriction = 0.4f;
            bounceMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            
            // Apply physics material to physics collider only
            if (physicsCollider != null)
            {
                physicsCollider.material = bounceMaterial;
            }

            // Ignore collision between coin physics collider and player colliders
            // This allows player to walk through coins while still detecting the trigger
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && physicsCollider != null)
            {
                Collider[] playerColliders = playerObj.GetComponents<Collider>();
                foreach (Collider playerCol in playerColliders)
                {
                    if (playerCol != null && !playerCol.isTrigger)
                    {
                        Physics.IgnoreCollision(physicsCollider, playerCol, true);
                    }
                }
            }
            
            // Calculate explosion direction for this coin
            Vector3 coinDirection = explosionDirection;
            if (coinDirection == Vector3.zero)
            {
                // Random direction if no player
                float angle = (360f / coinDrop) * i * Mathf.Deg2Rad;
                coinDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            }
            else
            {
                // Spread around explosion direction
                float spreadAngle = Random.Range(-coinRandomSpread, coinRandomSpread) * Mathf.Deg2Rad;
                Quaternion spreadRotation = Quaternion.Euler(0, spreadAngle, 0);
                coinDirection = spreadRotation * coinDirection;
            }
            
            // Add random horizontal spread
            coinDirection.x += Random.Range(-0.3f, 0.3f);
            coinDirection.z += Random.Range(-0.3f, 0.3f);
            coinDirection.Normalize();
            
            // Apply explosion force (horizontal + upward)
            Vector3 force = coinDirection * coinExplosionForce;
            force.y = coinUpwardForce; // Always add upward force
            coinRb.AddForce(force, ForceMode.Impulse);
            
            // Add random rotation
            coinRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }

    private void DropSouls()
    {
        if (soulDrop <= 0) return;

        var player = FindObjectOfType<PlayerSoul>();
        if (player != null)
        {
            player.AddSouls(soulDrop);
        }
    }
}
