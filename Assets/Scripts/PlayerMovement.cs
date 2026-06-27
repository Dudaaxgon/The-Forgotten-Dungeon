using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float speed = 3f;
    [SerializeField] public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public Vector2 movement;

    private Vector2 lastDirection = Vector2.down;
    private PlayerCombat2D combat;

    public Vector2 LastDirection => lastDirection;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        ConfigurePhysics();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        ConfigurePhysics();

        if (GetComponent<PlayerCombat2D>() == null)
        {
            combat = gameObject.AddComponent<PlayerCombat2D>();
        }
        else
        {
            combat = GetComponent<PlayerCombat2D>();
        }
    }

    private void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = Vector2.ClampMagnitude(movement, 1f);

        if (movement.sqrMagnitude > 0.001f)
        {
            lastDirection = movement.normalized;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void ConfigurePhysics()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.55f, 0.8f);
            capsule.offset = new Vector2(0f, 0.05f);
            bodyCollider = capsule;
        }

        bodyCollider.isTrigger = false;
        bodyCollider.usedByComposite = false;
    }

    private void UpdateAnimator()
    {
        if (anim == null)
        {
            return;
        }

        if (combat != null && combat.IsAttackAnimationActive)
        {
            return;
        }

        var action = movement.sqrMagnitude > 0.001f ? "Walk" : "Idle";
        var direction = GetDirectionName(lastDirection);
        var stateName = $"{action}_{direction}";
        var stateHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (anim.HasState(0, stateHash) && !anim.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            anim.Play(stateName);
        }
    }

    private static string GetDirectionName(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? "Right" : "Left";
        }

        return direction.y >= 0f ? "Up" : "Down";
    }
}
