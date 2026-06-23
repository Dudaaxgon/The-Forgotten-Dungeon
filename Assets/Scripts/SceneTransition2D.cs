using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SceneTransition2D : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private Vector2 destinationPosition;
    private bool transitioning;

    public void Configure(string sceneName, Vector2 spawnPosition)
    {
        targetScene = sceneName;
        destinationPosition = spawnPosition;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transitioning)
        {
            return;
        }

        var player = other.attachedRigidbody != null
            ? other.attachedRigidbody.GetComponent<PlayerHealth>()
            : other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return;
        }

        transitioning = true;
        PhaseProgressionBootstrap.TransitionPlayer(player, targetScene, destinationPosition);
    }
}
