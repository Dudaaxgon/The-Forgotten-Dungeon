using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 35;
    [SerializeField] private int currentHealth;
    [SerializeField, Min(0f)] private float destroyAfterDeathSeconds = 1.1f;

    private Animator animator;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public event Action<EnemyHealth2D> Died;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    public void Configure(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        isDead = false;
    }

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (amount <= 0 || isDead)
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth > 0)
        {
            PlayDirectionalState("Hurt", sourcePosition);
            return true;
        }

        Die(sourcePosition);
        return true;
    }

    private void Die(Vector2 sourcePosition)
    {
        isDead = true;
        PlayDirectionalState("Death", sourcePosition);

        var ai = GetComponent<EnemyAI2D>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        var body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        foreach (var bodyCollider in GetComponents<Collider2D>())
        {
            bodyCollider.enabled = false;
        }

        Died?.Invoke(this);
        Destroy(gameObject, destroyAfterDeathSeconds);
    }

    private void PlayDirectionalState(string action, Vector2 sourcePosition)
    {
        if (animator == null)
        {
            return;
        }

        var direction = (Vector2)transform.position - sourcePosition;
        var directionName = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x >= 0f ? "Right" : "Left")
            : (direction.y >= 0f ? "Up" : "Down");
        var stateName = $"{action}_{directionName}";
        var stateHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateName);
        }
    }
}
