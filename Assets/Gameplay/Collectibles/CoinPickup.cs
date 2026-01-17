using UnityEngine;
using DG.Tweening;

/// <summary>
/// Coin Pickup - World object that gives coins to player
/// Coins are stackable currency (separate from souls)
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int value = 1;

    [Header("Animation Settings")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float collectScale = 1.5f;
    [SerializeField] private float collectDuration = 0.3f;

    [Header("Physics Settings")]
    [SerializeField] private float physicsSettleVelocity = 0.5f; // Velocity threshold to consider coin "settled"
    [SerializeField] private float physicsSettleDelay = 0.5f; // Additional delay after settling before starting animation

    private Vector3 startPosition;
    private bool isCollected = false;
    private bool startPositionSet = false;
    private bool animationsStarted = false;
    private Tween floatTween;
    private Tween rotationTween;
    private Rigidbody rb;
    private float settleTimer = 0f;

    private void Start()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"CoinPickup on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }

        // Check if coin has Rigidbody (physics-based coin from enemy death)
        rb = GetComponent<Rigidbody>();
        
        // Set start position for floating animation
        if (!startPositionSet)
        {
            startPosition = transform.position;
            startPositionSet = true;
        }

        // If coin has Rigidbody and is moving, wait for it to settle
        if (rb != null && !rb.isKinematic)
        {
            // Don't start animations yet - wait for physics to settle
            return;
        }

        // Start animations immediately if no physics
        StartAnimations();
    }

    private void Update()
    {
        // If coin has physics, wait for it to settle before starting animations
        if (!animationsStarted && rb != null && !rb.isKinematic)
        {
            // Check if velocity is low enough (coin has settled)
            if (rb.linearVelocity.magnitude < physicsSettleVelocity)
            {
                settleTimer += Time.deltaTime;
                
                // Wait additional delay after settling
                if (settleTimer >= physicsSettleDelay)
                {
                    // Coin has settled, update start position and start animations
                    startPosition = transform.position;
                    startPositionSet = true;
                    
                    // Make Rigidbody kinematic so DOTween can control it
                    rb.isKinematic = true;
                    
                    StartAnimations();
                }
            }
            else
            {
                // Reset timer if coin is still moving
                settleTimer = 0f;
            }
        }
    }

    private void StartAnimations()
    {
        if (animationsStarted) return;
        animationsStarted = true;

        // Start floating animation
        StartFloatingAnimation();
        
        // Start rotation animation
        StartRotationAnimation();
    }

    /// <summary>
    /// Start floating animation using DOTween
    /// </summary>
    private void StartFloatingAnimation()
    {
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
        }

        // Create a smooth floating animation
        floatTween = transform.DOMoveY(startPosition.y + floatAmplitude, 1f / floatSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// Start rotation animation using DOTween
    /// </summary>
    private void StartRotationAnimation()
    {
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }

        // Create continuous rotation animation
        rotationTween = transform.DORotate(new Vector3(0, 360, 0), 360f / rotationSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        PlayerCurrency playerCurrency = other.GetComponent<PlayerCurrency>();
        if (playerCurrency != null)
        {
            CollectCoin();
        }
        else
        {
            Debug.LogWarning($"CoinPickup: Player does not have PlayerCurrency component!");
        }
    }

    /// <summary>
    /// Collect the coin with animation
    /// </summary>
    private void CollectCoin()
    {
        if (isCollected) return;
        isCollected = true;

        // Stop existing animations
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
        }
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }

        // Disable collider to prevent multiple collections
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Get PlayerCurrency and add coins
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerCurrency playerCurrency = player.GetComponent<PlayerCurrency>();
            if (playerCurrency != null)
            {
                playerCurrency.AddCoins(value);
            }
        }

        // Play coin collection sound
        if (AudioManager.Instance != null)
        {
            // Add slight pitch variation to avoid repetition
            float pitch = Random.Range(0.95f, 1.05f);
            AudioManager.Instance.PlaySFX("Coin", volume: 0.8f, pitch: pitch);
        }

        // Create collect animation sequence
        Sequence collectSequence = DOTween.Sequence();

        // Scale up animation
        collectSequence.Append(transform.DOScale(transform.localScale * collectScale, collectDuration * 0.5f)
            .SetEase(Ease.OutBack));

        // Fade out and scale down
        // Try SpriteRenderer first (for 2D coins)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            collectSequence.Join(spriteRenderer.DOFade(0f, collectDuration * 0.5f));
        }
        else
        {
            // Try MeshRenderer (for 3D coins)
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to fade material if it supports it
                Material mat = renderer.material;
                if (mat != null && mat.HasProperty("_Color"))
                {
                    collectSequence.Join(renderer.material.DOFade(0f, collectDuration * 0.5f));
                }
            }
        }

        // Scale down while fading
        collectSequence.Append(transform.DOScale(Vector3.zero, collectDuration * 0.5f)
            .SetEase(Ease.InBack));

        // Destroy after animation completes
        collectSequence.OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        // Clean up tweens
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
        }
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }
    }
}
