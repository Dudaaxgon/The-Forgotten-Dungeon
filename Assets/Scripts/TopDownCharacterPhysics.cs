using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TopDownCharacterPhysics : MonoBehaviour
{
    [SerializeField] private bool configureOnAwake = true;

    private void Reset()
    {
        Configure();
    }

    private void Awake()
    {
        if (configureOnAwake)
        {
            Configure();
        }
    }

    public void Configure()
    {
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = GetComponent<Collider2D>();
        collider.isTrigger = false;
        collider.usedByComposite = false;
    }
}
