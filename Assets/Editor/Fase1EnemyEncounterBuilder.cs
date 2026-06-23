using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Fase1EnemyEncounterBuilder
{
    private const string ScenePath = "Assets/Scenes/Fase1.unity";
    private const string CharacterRoot = "Assets/Characters/AutoImported";

    private readonly struct EnemyPlacement
    {
        public EnemyPlacement(string encounter, string character, Vector2 position, float speed, float detection, float leash, int damage)
        {
            Encounter = encounter;
            Character = character;
            Position = position;
            Speed = speed;
            Detection = detection;
            Leash = leash;
            Damage = damage;
        }

        public string Encounter { get; }
        public string Character { get; }
        public Vector2 Position { get; }
        public float Speed { get; }
        public float Detection { get; }
        public float Leash { get; }
        public int Damage { get; }
    }

    private static readonly EnemyPlacement[] Placements =
    {
        new("01_Antecâmara", "Goblin1", new Vector2(8f, 57f), 1.65f, 4.5f, 7f, 8),
        new("02_Gargalo_Superior", "Skeleton1", new Vector2(2f, 50f), 1.7f, 4.8f, 7f, 9),
        new("03_Grande_Salão", "Goblin2", new Vector2(-8f, 38f), 1.8f, 5f, 8f, 9),
        new("03_Grande_Salão", "Skeleton2", new Vector2(3f, 36f), 1.75f, 5f, 8f, 10),
        new("03_Grande_Salão", "Zombie1", new Vector2(9f, 29f), 1.35f, 4.5f, 7f, 12),
        new("04_Celas_Leste", "Zombie2", new Vector2(29f, 31f), 1.35f, 4.5f, 7f, 12),
        new("04_Celas_Leste", "Skeleton1", new Vector2(45f, 28f), 1.75f, 5f, 8f, 9),
        new("05_Cruzamento", "Orc1", new Vector2(12f, 18f), 1.55f, 5.5f, 9f, 14),
        new("06_Sala_da_Guarda", "Goblin3", new Vector2(-2f, 10f), 1.95f, 5.2f, 8f, 10),
        new("06_Sala_da_Guarda", "Skeleton2", new Vector2(5f, 7f), 1.75f, 5f, 8f, 10),
        new("07_Celas_Oeste", "Zombie2", new Vector2(-31f, 5f), 1.35f, 4.5f, 7f, 12),
        new("07_Celas_Oeste", "Skeleton3", new Vector2(-20f, 1f), 1.8f, 5f, 8f, 11),
        new("08_Cripta_Leste", "Orc2", new Vector2(34f, 7f), 1.6f, 5.5f, 9f, 15),
        new("08_Cripta_Leste", "Zombie3", new Vector2(42f, 3f), 1.4f, 4.8f, 8f, 14),
        new("09_Galeria_Central", "Orc3", new Vector2(18f, -6f), 1.65f, 5.8f, 9f, 16),
        new("09_Galeria_Central", "Skeleton3", new Vector2(12f, -12f), 1.85f, 5.2f, 8f, 11),
        new("10_Câmara_Final", "Orc2", new Vector2(0f, -22f), 1.6f, 5.5f, 9f, 15),
        new("10_Câmara_Final", "Zombie3", new Vector2(13f, -23f), 1.4f, 5f, 8f, 14),
        new("10_Câmara_Final", "Skeleton3", new Vector2(24f, -20f), 1.85f, 5.5f, 9f, 11)
    };

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RemoveExistingGameplayRoot(scene);
        TopDownSceneCollisionBootstrap.ConfigureSceneColliders(scene);

        var ground = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => tilemap.name == "Chao");
        if (ground == null)
        {
            throw new InvalidOperationException("The Chao tilemap was not found in Fase1.");
        }

        var gameplayRoot = new GameObject("Gameplay_Fase1");
        SceneManager.MoveGameObjectToScene(gameplayRoot, scene);

        var player = CreatePlayer(ground, gameplayRoot.transform);
        ConfigureCamera(scene, player.transform);
        CreateEnemies(ground, gameplayRoot.transform);
        ConfigureBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Fase1 enemy encounters built successfully. Enemies: {Placements.Length}.");
    }

    private static GameObject CreatePlayer(Tilemap ground, Transform parent)
    {
        var prefab = LoadCharacterPrefab("Swordsman_lvl1");
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Player_Swordsman";
        player.tag = "Player";
        player.transform.SetParent(parent);
        player.transform.position = SnapToFloor(ground, new Vector2(-8f, 57f));
        EnsureComponent<PlayerMovement>(player);
        EnsureComponent<PlayerHealth>(player);
        SetCharacterRendering(player);
        return player;
    }

    private static void CreateEnemies(Tilemap ground, Transform gameplayRoot)
    {
        var enemiesRoot = new GameObject("Enemies").transform;
        enemiesRoot.SetParent(gameplayRoot);
        var encounterRoots = new Dictionary<string, Transform>();
        var counters = new Dictionary<string, int>();

        foreach (var placement in Placements)
        {
            if (!encounterRoots.TryGetValue(placement.Encounter, out var encounterRoot))
            {
                encounterRoot = new GameObject(placement.Encounter).transform;
                encounterRoot.SetParent(enemiesRoot);
                encounterRoots.Add(placement.Encounter, encounterRoot);
            }

            counters.TryGetValue(placement.Character, out var count);
            counters[placement.Character] = ++count;

            var prefab = LoadCharacterPrefab(placement.Character);
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.name = $"Enemy_{placement.Character}_{count:00}";
            enemy.transform.SetParent(encounterRoot);
            enemy.transform.position = SnapToFloor(ground, placement.Position);
            var ai = EnsureComponent<EnemyAI2D>(enemy);
            ai.Configure(placement.Speed, placement.Detection, placement.Leash, placement.Damage);
            SetCharacterRendering(enemy);
        }
    }

    private static void ConfigureCamera(Scene scene, Transform player)
    {
        var camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"))
            ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            throw new InvalidOperationException("Fase1 does not contain a camera.");
        }

        var follow = EnsureComponent<CameraFollow2D>(camera.gameObject);
        follow.SetTarget(player);
        camera.transform.position = new Vector3(player.position.x, player.position.y, camera.transform.position.z);
    }

    private static Vector3 SnapToFloor(Tilemap ground, Vector2 desired)
    {
        var center = ground.WorldToCell(desired);
        for (var radius = 0; radius <= 8; radius++)
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
                    if (!Physics2D.OverlapCircle(world, 0.3f))
                    {
                        return new Vector3(world.x, world.y, 0f);
                    }
                }
            }
        }

        throw new InvalidOperationException($"No walkable floor cell was found near {desired}.");
    }

    private static GameObject LoadCharacterPrefab(string character)
    {
        var path = $"{CharacterRoot}/{character}/Prefabs/{character}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Character prefab was not found: {path}");
        }
        return prefab;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        return target.GetComponent<T>() ?? target.AddComponent<T>();
    }

    private static void SetCharacterRendering(GameObject character)
    {
        foreach (var renderer in character.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = 20;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }
    }

    private static void RemoveExistingGameplayRoot(Scene scene)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Gameplay_Fase1");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static void ConfigureBuildSettings()
    {
        var current = EditorBuildSettings.scenes.ToList();
        current.RemoveAll(entry => entry.path == ScenePath);
        current.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = current.ToArray();
    }
}
