using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI2D : MonoBehaviour
{
    private enum State
    {
        Guard,
        Chase,
        Attack,
        Return
    }

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.8f;
    [SerializeField, Min(0.5f)] private float detectionRadius = 5f;
    [SerializeField, Min(1f)] private float leashRadius = 8f;
    [SerializeField, Min(0.1f)] private float attackRange = 0.9f;

    [Header("Combat")]
    [SerializeField, Min(1)] private int attackDamage = 10;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.15f;
    [SerializeField, Range(0f, 1f)] private float attackHitDelay = 0.28f;
    [SerializeField, Min(0.05f)] private float attackAnimationLock = 0.45f;

    private Rigidbody2D body;
    private Animator animator;
    private EnemyHealth2D health;
    private Collider2D[] enemyColliders;
    private PlayerHealth ignoredCollisionPlayer;
    private PlayerHealth target;
    private Vector2 guardPosition;
    private Vector2 facing = Vector2.down;
    private State state;
    private float nextAttackTime;
    private float pendingHitTime = -1f;
    private float attackAnimationUntil;
    private string activeAnimation;

    public Vector2 GuardPosition => guardPosition;
    public float DetectionRadius => detectionRadius;
    public float LeashRadius => leashRadius;
    public int AttackDamage => attackDamage;
    public bool HasTarget => target != null && !target.IsDead;

    public void Configure(float speed, float detection, float leash, int damage)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
        detectionRadius = Mathf.Max(0.5f, detection);
        leashRadius = Mathf.Max(detectionRadius, leash);
        attackDamage = Mathf.Max(1, damage);
    }

    public void SetGuardPosition(Vector2 position)
    {
        guardPosition = position;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth2D>() ?? gameObject.AddComponent<EnemyHealth2D>();
        enemyColliders = GetComponentsInChildren<Collider2D>(true);
        guardPosition = body.position;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        target = null;
        pendingHitTime = -1f;
        attackAnimationUntil = 0f;
        nextAttackTime = 0f;
        activeAnimation = null;
        ignoredCollisionPlayer = null;
        IgnoreCollisionsWithPlayer(Object.FindFirstObjectByType<PlayerHealth>());
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        if (pendingHitTime >= 0f && Time.time >= pendingHitTime)
        {
            pendingHitTime = -1f;
            ResolveAttackHit();
        }

        if (!HasTarget)
        {
            target = FindPlayer();
            IgnoreCollisionsWithPlayer(target);
        }

        UpdateState();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (state == State.Chase && HasTarget)
        {
            MoveTowards(target.transform.position);
        }
        else if (state == State.Return)
        {
            MoveTowards(guardPosition);
        }
    }

    private void UpdateState()
    {
        if (!HasTarget)
        {
            state = Vector2.Distance(body.position, guardPosition) > 0.08f ? State.Return : State.Guard;
            return;
        }

        var targetPosition = (Vector2)target.transform.position;
        var distanceFromGuard = Vector2.Distance(targetPosition, guardPosition);
        var distanceToTarget = Vector2.Distance(body.position, targetPosition);

        if (distanceFromGuard > leashRadius)
        {
            target = null;
            state = State.Return;
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            state = State.Attack;
            FaceTarget(targetPosition);
            BeginAttack();
        }
        else if (distanceToTarget <= detectionRadius && HasLineOfSight(targetPosition))
        {
            state = State.Chase;
        }
        else
        {
            target = null;
            state = State.Return;
        }
    }

    private PlayerHealth FindPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return null;
        }

        var playerPosition = (Vector2)player.transform.position;
        return Vector2.Distance(body.position, playerPosition) <= detectionRadius && HasLineOfSight(playerPosition)
            ? player
            : null;
    }

    private void IgnoreCollisionsWithPlayer(PlayerHealth player)
    {
        if (player == null || player == ignoredCollisionPlayer)
        {
            return;
        }

        if (enemyColliders == null || enemyColliders.Length == 0)
        {
            enemyColliders = GetComponentsInChildren<Collider2D>(true);
        }

        foreach (var enemyCollider in enemyColliders)
        {
            if (enemyCollider == null)
            {
                continue;
            }

            foreach (var playerCollider in player.GetComponentsInChildren<Collider2D>(true))
            {
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(enemyCollider, playerCollider, true);
                }
            }
        }

        ignoredCollisionPlayer = player;
    }

    private bool HasLineOfSight(Vector2 targetPosition)
    {
        var hits = Physics2D.LinecastAll(body.position, targetPosition);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform == transform || hit.collider.GetComponent<PlayerHealth>() != null)
            {
                continue;
            }

            if (hit.collider.GetComponent<EnemyAI2D>() == null)
            {
                return false;
            }
        }

        return true;
    }

    private void MoveTowards(Vector2 destination)
    {
        var delta = destination - body.position;
        if (delta.sqrMagnitude <= 0.0064f)
        {
            if (state == State.Return)
            {
                state = State.Guard;
            }
            return;
        }

        facing = delta.normalized;
        body.MovePosition(body.position + facing * moveSpeed * Time.fixedDeltaTime);
    }

    private void BeginAttack()
    {
        if (Time.time < nextAttackTime || pendingHitTime >= 0f)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        pendingHitTime = Time.time + attackHitDelay;
        attackAnimationUntil = Time.time + attackAnimationLock;
        PlayAnimation("Attack");
    }

    private void ResolveAttackHit()
    {
        if (!HasTarget)
        {
            return;
        }

        var targetPosition = (Vector2)target.transform.position;
        if (Vector2.Distance(body.position, targetPosition) <= attackRange + 0.2f && HasLineOfSight(targetPosition))
        {
            target.TakeDamage(attackDamage, body.position);
        }
    }

    private void UpdateAnimation()
    {
        if (state == State.Attack && Time.time < attackAnimationUntil)
        {
            return;
        }

        PlayAnimation(state == State.Chase || state == State.Return ? "Walk" : "Idle");
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        var targetDirection = targetPosition - body.position;
        if (targetDirection.sqrMagnitude > 0.001f)
        {
            facing = targetDirection.normalized;
        }
    }

    private void PlayAnimation(string action)
    {
        if (animator == null)
        {
            return;
        }

        var direction = GetDirectionName();
        var stateName = $"{action}_{direction}";
        if (stateName == activeAnimation)
        {
            return;
        }

        var stateHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateName, 0, 0f);
            activeAnimation = stateName;
        }
    }

    private string GetDirectionName()
    {
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            return facing.x >= 0f ? "Right" : "Left";
        }

        return facing.y >= 0f ? "Down" : "Up";
    }
}
