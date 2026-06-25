using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class PhasePlayerBuilder
{
    private const string PlayerName = "Player_Swordsman";
    private const string PlayerPrefabPath = "Assets/Characters/AutoImported/Swordsman_lvl1/Prefabs/Swordsman_lvl1.prefab";

    private readonly struct PhaseSpec
    {
        public PhaseSpec(string scenePath, Vector2 spawn)
        {
            ScenePath = scenePath;
            Spawn = spawn;
        }

        public string ScenePath { get; }
        public Vector2 Spawn { get; }
    }

    private static readonly PhaseSpec[] Phases =
    {
        new("Assets/Scenes/Fase1.unity", new Vector2(-4.5f, 56.5f)),
        new("Assets/Scenes/Fase2.unity", PhaseProgressionBootstrap.PhaseTwoEntryPosition),
        new("Assets/Scenes/Fase3.unity", PhaseProgressionBootstrap.PhaseThreeEntryPosition),
        new("Assets/Scenes/Fase4.unity", PhaseProgressionBootstrap.PhaseFourEntryPosition)
    };

    [MenuItem("Tools/Top Down/Build Phase Players")]
    public static void Build()
    {
        foreach (var phase in Phases)
        {
            BuildScene(phase);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Phase player build completed. Scenes={Phases.Length}.");
    }

    private static void BuildScene(PhaseSpec phase)
    {
        var scene = EditorSceneManager.OpenScene(phase.ScenePath, OpenSceneMode.Single);
        TopDownSceneCollisionBootstrap.ConfigureSceneColliders(scene);

        foreach (var tilemapCollider in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>(true)))
        {
            tilemapCollider.ProcessTilemapChanges();
        }

        Physics2D.SyncTransforms();

        var ground = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => tilemap.name == "Chao");
        if (ground == null)
        {
            throw new InvalidOperationException($"Chao tilemap not found in {phase.ScenePath}.");
        }

        var players = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerMovement>(true))
            .Select(component => component.gameObject)
            .Distinct()
            .ToArray();

        GameObject player;
        if (players.Length == 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Player prefab not found: {PlayerPrefabPath}");
            }

            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            SceneManager.MoveGameObjectToScene(player, scene);
            var gameplayRoot = FindOrCreateGameplayRoot(scene);
            player.transform.SetParent(gameplayRoot.transform);
        }
        else
        {
            player = players[0];
            for (var index = 1; index < players.Length; index++)
            {
                UnityEngine.Object.DestroyImmediate(players[index]);
            }
        }

        ConfigurePlayer(player, ground, phase.Spawn);
        ConfigureCamera(scene, player.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save {phase.ScenePath}.");
        }

        Debug.Log($"Player ready in {phase.ScenePath}: {player.name} at {(Vector2)player.transform.position}.");
    }

    private static GameObject FindOrCreateGameplayRoot(Scene scene)
    {
        var expectedName = $"Gameplay_{scene.name}";
        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == expectedName);
        if (existing != null)
        {
            return existing;
        }

        var root = new GameObject(expectedName);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static void ConfigurePlayer(GameObject player, Tilemap ground, Vector2 spawn)
    {
        player.name = PlayerName;
        player.tag = "Player";
        player.transform.position = FindSafePosition(ground, spawn, player);

        var movement = player.GetComponent<PlayerMovement>() ?? player.AddComponent<PlayerMovement>();
        var health = player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();
        var body = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
        var collider = player.GetComponent<Collider2D>() ?? player.AddComponent<CapsuleCollider2D>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        collider.isTrigger = false;
        collider.usedByComposite = false;

        if (collider is CapsuleCollider2D capsule)
        {
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.55f, 0.8f);
            capsule.offset = new Vector2(0f, 0.05f);
        }

        movement.rb = body;
        movement.anim = player.GetComponent<Animator>();
        movement.enabled = true;
        health.enabled = true;

        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = 20;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        Physics2D.SyncTransforms();
    }

    private static void ConfigureCamera(Scene scene, Transform player)
    {
        var camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"))
            ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            return;
        }

        var follow = camera.GetComponent<CameraFollow2D>() ?? camera.gameObject.AddComponent<CameraFollow2D>();
        follow.SetTarget(player);
        camera.transform.position = new Vector3(player.position.x, player.position.y, camera.transform.position.z);
    }

    private static Vector3 FindSafePosition(Tilemap ground, Vector2 desired, GameObject player)
    {
        var center = ground.WorldToCell(desired);
        for (var radius = 0; radius <= 16; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                    {
                        continue;
                    }

                    var cell = center + new Vector3Int(x, y, 0);
                    if (!ground.HasTile(cell))
                    {
                        continue;
                    }

                    var world = ground.GetCellCenterWorld(cell);
                    var blocked = Physics2D.OverlapCircleAll(world, 0.35f)
                        .Any(collider => collider != null
                            && !collider.isTrigger
                            && collider.transform.root != player.transform.root
                            && collider.GetComponentInParent<PlayerMovement>() == null);
                    if (!blocked)
                    {
                        return new Vector3(world.x, world.y, 0f);
                    }
                }
            }
        }

        throw new InvalidOperationException($"No safe player spawn found near {desired}.");
    }
}
