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
    }

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (amount <= 0 || IsDead || Time.time < nextDamageTime)
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        nextDamageTime = Time.time + damageImmunitySeconds;
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
        }

        return true;
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
