using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhaseProgressionBootstrap
{
    public const string PhaseOneScene = "Fase1";
    public const string PhaseTwoScene = "Fase2";
    public const string PhaseThreeScene = "Fase3";
    public const string PhaseFourScene = "Fase4";
    public static readonly Vector2 PhaseOneExitPosition = new(8f, -18f);
    public static readonly Vector2 PhaseTwoEntryPosition = new(-79.5f, -19.5f);
    public static readonly Vector2 PhaseTwoExitPosition = new(62.5f, -21.5f);
    public static readonly Vector2 PhaseThreeEntryPosition = new(-20.5f, -72.5f);
    public static readonly Vector2 PhaseThreeExitPosition = new(24.5f, 6.5f);
    public static readonly Vector2 PhaseFourEntryPosition = new(-44.5f, -28.5f);

    private static PlayerHealth pendingPlayer;
    private static Vector2 pendingPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureScene(SceneManager.GetActiveScene());
    }

    public static void TransitionPlayer(PlayerHealth player, string targetScene, Vector2 destinationPosition)
    {
        if (player == null || string.IsNullOrWhiteSpace(targetScene))
        {
            return;
        }

        pendingPlayer = player;
        pendingPosition = destinationPosition;
        player.transform.SetParent(null, true);
        Object.DontDestroyOnLoad(player.gameObject);

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
            movement.movement = Vector2.zero;
        }

        SceneManager.LoadScene(targetScene);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (scene.name == PhaseOneScene)
        {
            EnsurePhaseOneExit(scene);
        }

        if (scene.name == PhaseTwoScene)
        {
            EnsurePhaseTwoExit(scene);
            PlacePlayerInPhase(PhaseTwoEntryPosition);
        }

        if (scene.name == PhaseThreeScene)
        {
            EnsurePhaseThreeExit(scene);
            PlacePlayerInPhase(PhaseThreeEntryPosition);
        }

        if (scene.name == PhaseFourScene)
        {
            PlacePlayerInPhase(PhaseFourEntryPosition);
        }
    }

    private static void EnsurePhaseOneExit(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Fase1ExitToFase2")
            {
                return;
            }
        }

        var exitObject = new GameObject("Fase1ExitToFase2");
        SceneManager.MoveGameObjectToScene(exitObject, scene);
        exitObject.transform.position = PhaseOneExitPosition;
        var trigger = exitObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2f, 2f);
        trigger.isTrigger = true;
        var transition = exitObject.AddComponent<SceneTransition2D>();
        transition.Configure(PhaseTwoScene, PhaseTwoEntryPosition);
    }

    private static void EnsurePhaseTwoExit(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Fase2ExitToFase3")
            {
                return;
            }
        }

        var exitObject = new GameObject("Fase2ExitToFase3");
        SceneManager.MoveGameObjectToScene(exitObject, scene);
        exitObject.transform.position = PhaseTwoExitPosition;
        var trigger = exitObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2f, 3f);
        trigger.isTrigger = true;
        var transition = exitObject.AddComponent<SceneTransition2D>();
        transition.Configure(PhaseThreeScene, PhaseThreeEntryPosition);
    }

    private static void EnsurePhaseThreeExit(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Fase3ExitToFase4")
            {
                return;
            }
        }

        var exitObject = new GameObject("Fase3ExitToFase4");
        SceneManager.MoveGameObjectToScene(exitObject, scene);
        exitObject.transform.position = PhaseThreeExitPosition;
        var trigger = exitObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(3f, 2f);
        trigger.isTrigger = true;
        var transition = exitObject.AddComponent<SceneTransition2D>();
        transition.Configure(PhaseFourScene, PhaseFourEntryPosition);
    }

    private static void PlacePlayerInPhase(Vector2 defaultPosition)
    {
        var player = pendingPlayer != null ? pendingPlayer : Object.FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            return;
        }

        var position = pendingPlayer != null ? pendingPosition : defaultPosition;
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && !player.IsDead)
        {
            movement.enabled = true;
        }

        var camera = Camera.main;
        if (camera != null)
        {
            var follow = camera.GetComponent<CameraFollow2D>() ?? camera.gameObject.AddComponent<CameraFollow2D>();
            follow.SetTarget(player.transform);
            camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }

        pendingPlayer = null;
    }
}
