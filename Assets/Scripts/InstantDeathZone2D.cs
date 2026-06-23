using UnityEngine;

[DisallowMultipleComponent]
public class InstantDeathZone2D : MonoBehaviour
{
    private void Awake()
    {
        foreach (var zoneCollider in GetComponents<Collider2D>())
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var playerHealth = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<PlayerHealth>()
            : other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Kill(transform.position);
        }
    }
}
