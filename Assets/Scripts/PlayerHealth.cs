using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField, Min(0f)] private float damageImmunitySeconds = 0.35f;

    private Animator animator;
    private float nextDamageTime;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        if (GetComponent<PlayerGameOver2D>() == null)
        {
            gameObject.AddComponent<PlayerGameOver2D>();
        }
    }

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (amount <= 0 || IsDead || Time.time < nextDamageTime)
        {
            return false;
        }

        SetHealth(Mathf.Max(0, currentHealth - amount), sourcePosition);
        nextDamageTime = Time.time + damageImmunitySeconds;

        return true;
    }

    public bool Kill(Vector2 sourcePosition)
    {
        if (IsDead)
        {
            return false;
        }

        SetHealth(0, sourcePosition);
        return true;
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        nextDamageTime = 0f;
        HealthChanged?.Invoke(currentHealth, maxHealth);

        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    private void SetHealth(int value, Vector2 sourcePosition)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        var direction = (Vector2)transform.position - sourcePosition;
        PlayDirectionalState(IsDead ? "Death" : "Hurt", direction);

        if (IsDead)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }

    private void PlayDirectionalState(string action, Vector2 direction)
    {
        if (animator == null)
        {
            return;
        }

        var directionName = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x >= 0f ? "Right" : "Left")
            : (direction.y >= 0f ? "Up" : "Down");
        var stateName = $"{action}_{directionName}";
        if (animator.HasState(0, Animator.StringToHash($"Base Layer.{stateName}")))
        {
            animator.Play(stateName);
        }
    }
}
