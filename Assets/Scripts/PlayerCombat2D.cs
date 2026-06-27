using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerCombat2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int attackDamage = 18;
    [SerializeField, Min(0.1f)] private float attackRange = 0.95f;
    [SerializeField, Min(0.1f)] private float attackRadius = 0.55f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.45f;
    [SerializeField] private KeyCode keyboardAttackKey = KeyCode.Space;

    private PlayerMovement movement;
    private Animator animator;
    private float nextAttackTime;

    public int AttackDamage => attackDamage;
    public float AttackRange => attackRange;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(keyboardAttackKey))
        {
            TryAttack();
        }
    }

    public bool TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;
        var direction = movement != null ? movement.LastDirection : Vector2.down;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        PlayAttackAnimation(direction);

        var origin = (Vector2)transform.position;
        var hitCenter = origin + direction * attackRange;
        var candidates = Physics2D.OverlapCircleAll(hitCenter, attackRadius)
            .Select(hit => hit.attachedRigidbody != null
                ? hit.attachedRigidbody.GetComponent<EnemyHealth2D>()
                : hit.GetComponentInParent<EnemyHealth2D>())
            .Where(enemy => enemy != null && !enemy.IsDead)
            .Distinct()
            .OrderBy(enemy => Vector2.Distance(origin, enemy.transform.position))
            .ToArray();

        foreach (var enemy in candidates)
        {
            if (!HasLineOfSight(origin, enemy.transform.position))
            {
                continue;
            }

            enemy.TakeDamage(attackDamage, origin);
            return true;
        }

        return false;
    }

    private bool HasLineOfSight(Vector2 origin, Vector2 targetPosition)
    {
        var hits = Physics2D.LinecastAll(origin, targetPosition);
        foreach (var hit in hits)
        {
            if (hit.collider == null
                || hit.collider.isTrigger
                || hit.collider.GetComponentInParent<PlayerMovement>() != null
                || hit.collider.GetComponentInParent<EnemyHealth2D>() != null)
            {
                continue;
            }

            if (hit.collider.GetComponent<TilemapCollider2D>() != null || hit.collider.GetComponent<CompositeCollider2D>() != null)
            {
                return false;
            }
        }

        return true;
    }

    private void PlayAttackAnimation(Vector2 direction)
    {
        if (animator == null)
        {
            return;
        }

        var directionName = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x >= 0f ? "Right" : "Left")
            : (direction.y >= 0f ? "Up" : "Down");
        var candidates = new[]
        {
            $"Attack_{directionName}",
            $"WalkAttack_{directionName}",
            $"RunAttack_{directionName}"
        };

        foreach (var stateName in candidates)
        {
            var stateHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateName);
                return;
            }
        }
    }
}
