using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Hollow Knight style health system with discrete health points (masks)
/// Handles HP, damage, invincibility frames, knockback, and damage animations
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityTime = 1.0f;

    [Header("Damage Animation")]
    [SerializeField] private Transform visualTransform; // The visual model to animate (usually a child)
    [SerializeField] private float damageShakeDuration = 0.2f;
    [SerializeField] private float damageShakeStrength = 0.1f;
    [SerializeField] private int damageShakeVibrato = 10;
    [SerializeField] private float damageScalePunch = 0.9f;
    [SerializeField] private float damageScaleDuration = 0.15f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvincible { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private PlayerDeath playerDeath;
    private PlayerMovement playerMovement;
    private Coroutine invincibilityRoutine;
    private Vector3 originalVisualScale;
    private Vector3 originalVisualPosition;

    private void Awake()
    {
        playerDeath = GetComponent<PlayerDeath>();
        playerMovement = GetComponent<PlayerMovement>();
        CurrentHealth = maxHealth;

        // Auto-find visual transform if not assigned
        if (visualTransform == null)
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                visualTransform = renderer.transform;
            }
            else
            {
                visualTransform = transform;
            }
        }

        // Store original values for animation
        originalVisualScale = visualTransform.localScale;
        originalVisualPosition = visualTransform.localPosition;
    }

    /// <summary>
    /// Take damage (default 1 HP per hit)
    /// </summary>
    public void TakeDamage(int amount = 1)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

    /// <summary>
    /// Take damage with knockback
    /// </summary>
    public void TakeDamage(int amount, Vector3 knockbackDirection, float knockbackForce)
    {
        if (IsInvincible || IsDead) return;

        CurrentHealth -= amount;

        // Apply knockback
        if (knockbackForce > 0f && playerMovement != null)
        {
            playerMovement.ApplyKnockback(knockbackDirection, knockbackForce);
        }

        // Play damage animation
        PlayDamageAnimation();

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            if (playerDeath != null)
            {
                playerDeath.HandleDeath();
            }
            return;
        }

        StartInvincibility();
    }

    /// <summary>
    /// Check if player can heal (has less than max HP)
    /// </summary>
    public bool CanHeal()
    {
        return CurrentHealth < maxHealth;
    }

    /// <summary>
    /// Heal the player (default 1 HP)
    /// </summary>
    public void Heal(int amount = 1)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    /// <summary>
    /// Reset health to max (called on respawn)
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsInvincible = false;
        
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        // Reset visual transform
        if (visualTransform != null)
        {
            visualTransform.localScale = originalVisualScale;
            visualTransform.localPosition = originalVisualPosition;
        }

        // Kill any active tweens
        if (visualTransform != null)
        {
            visualTransform.DOKill();
        }
    }

    /// <summary>
    /// Play damage animation using DOTween
    /// </summary>
    private void PlayDamageAnimation()
    {
        if (visualTransform == null) return;

        // Kill any existing tweens
        visualTransform.DOKill();

        // Shake position
        visualTransform.DOShakePosition(
            damageShakeDuration,
            damageShakeStrength,
            damageShakeVibrato,
            90f,
            false,
            true
        );

        // Scale punch (squash effect)
        visualTransform.DOPunchScale(
            Vector3.one * (1f - damageScalePunch),
            damageScaleDuration,
            5,
            0.5f
        );
    }

    /// <summary>
    /// Start invincibility frames after taking damage
    /// </summary>
    private void StartInvincibility()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
        }
        
        invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
    }

    /// <summary>
    /// Invincibility frame routine with visual feedback
    /// </summary>
    private IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;

        // TODO: Add screen flash / shader effect / animation
        // For now, we can add a simple visual indicator
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Flash effect (simple version - can be enhanced with shaders)
            float elapsed = 0f;
            while (elapsed < invincibilityTime)
            {
                float alpha = Mathf.PingPong(elapsed * 10f, 1f);
                if (renderer.material.HasProperty("_Color"))
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Reset alpha
            if (renderer.material.HasProperty("_Color"))
            {
                Color color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
        }

        IsInvincible = false;
        invincibilityRoutine = null;
    }

    private void OnDestroy()
    {
        // Kill all tweens when component is destroyed
        if (visualTransform != null)
        {
            visualTransform.DOKill();
        }
    }
}
