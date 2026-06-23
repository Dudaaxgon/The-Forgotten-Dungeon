using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Fase2EnemyEncounterBuilder
{
    private const string ScenePath = "Assets/Scenes/Fase2.unity";
    private const string CharacterRoot = "Assets/Characters/AutoImported";
    private const string EnemyRootName = "Enemies_Fase2";

    private readonly struct Placement
    {
        public Placement(string encounter, string character, Vector2 position, float speed, float detection, float leash, int damage)
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

    private static readonly Placement[] Placements =
    {
        new("01_Entrada_Oeste", "Goblin1", new Vector2(-76f, -18f), 1.75f, 4.8f, 8f, 9),
        new("01_Entrada_Oeste", "Skeleton1", new Vector2(-72f, -22f), 1.75f, 5f, 8f, 10),
        new("01_Entrada_Oeste", "Slime1", new Vector2(-68f, -18f), 1.25f, 4.2f, 7f, 11),

        new("02_Ponte_Oeste", "Goblin2", new Vector2(-55f, 5f), 1.85f, 5f, 8f, 10),
        new("02_Ponte_Oeste", "Skeleton2", new Vector2(-49f, 7f), 1.8f, 5.2f, 8f, 11),
        new("02_Ponte_Oeste", "Zombie1", new Vector2(-45f, 3f), 1.4f, 4.8f, 8f, 12),

        new("03_Caverna_Central", "Orc1", new Vector2(-30f, -8f), 1.6f, 5.5f, 9f, 14),
        new("03_Caverna_Central", "Zombie2", new Vector2(-23f, -3f), 1.4f, 5f, 8f, 13),
        new("03_Caverna_Central", "Skeleton1", new Vector2(-18f, -12f), 1.8f, 5.2f, 8f, 10),
        new("03_Caverna_Central", "Slime2", new Vector2(-35f, 0f), 1.3f, 4.5f, 7f, 12),

        new("04_Cristais_Norte", "Skeleton2", new Vector2(-12f, 18f), 1.8f, 5.2f, 8f, 11),
        new("04_Cristais_Norte", "Orc2", new Vector2(-3f, 15f), 1.65f, 5.8f, 9f, 15),
        new("04_Cristais_Norte", "Golem1", new Vector2(5f, 20f), 1.15f, 5f, 8f, 17),

        new("05_Santuario_Leste", "Zombie3", new Vector2(25f, 20f), 1.45f, 5f, 8f, 14),
        new("05_Santuario_Leste", "Skeleton3", new Vector2(35f, 15f), 1.85f, 5.5f, 9f, 12),
        new("05_Santuario_Leste", "Lizardman1", new Vector2(43f, 18f), 1.7f, 5.8f, 9f, 15),

        new("06_Passagem_Leste", "Goblin3", new Vector2(50f, -5f), 1.95f, 5.3f, 8f, 11),
        new("06_Passagem_Leste", "Orc3", new Vector2(44f, -10f), 1.7f, 5.8f, 9f, 16),
        new("06_Passagem_Leste", "Lizardman2", new Vector2(55f, -15f), 1.75f, 6f, 9f, 16),

        new("07_Profundezas_Sul", "Skeleton3", new Vector2(15f, -30f), 1.85f, 5.5f, 9f, 12),
        new("07_Profundezas_Sul", "Zombie2", new Vector2(5f, -25f), 1.45f, 5f, 8f, 13),
        new("07_Profundezas_Sul", "Slime3", new Vector2(28f, -24f), 1.35f, 4.8f, 8f, 13),
        new("07_Profundezas_Sul", "Golem2", new Vector2(35f, -30f), 1.2f, 5.5f, 9f, 18)
    };

    [MenuItem("Tools/Top Down/Build Fase 2 Enemy Encounters")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TopDownSceneCollisionBootstrap.ConfigureSceneColliders(scene);
        foreach (var collider in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>(true)))
        {
            collider.ProcessTilemapChanges();
        }
        Physics2D.SyncTransforms();

        var ground = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => tilemap.name == "Chao");
        if (ground == null)
        {
            throw new InvalidOperationException("Fase2 Chao tilemap was not found.");
        }

        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnemyRootName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var enemyRoot = new GameObject(EnemyRootName);
        SceneManager.MoveGameObjectToScene(enemyRoot, scene);
        var encounterRoots = new Dictionary<string, Transform>();
        var counters = new Dictionary<string, int>();

        foreach (var placement in Placements)
        {
            if (!encounterRoots.TryGetValue(placement.Encounter, out var encounterRoot))
            {
                encounterRoot = new GameObject(placement.Encounter).transform;
                encounterRoot.SetParent(enemyRoot.transform);
                encounterRoots.Add(placement.Encounter, encounterRoot);
            }

            var safePosition = FindSafePosition(ground, placement.Position);
            counters.TryGetValue(placement.Character, out var count);
            counters[placement.Character] = ++count;
            var prefab = LoadCharacterPrefab(placement.Character);
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.name = $"Enemy_{placement.Character}_{count:00}";
            enemy.transform.SetParent(encounterRoot);
            enemy.transform.position = safePosition;
            var ai = enemy.GetComponent<EnemyAI2D>() ?? enemy.AddComponent<EnemyAI2D>();
            ai.Configure(placement.Speed, placement.Detection, placement.Leash, placement.Damage);
            foreach (var renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder = 20;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
            Physics2D.SyncTransforms();
            Debug.Log($"Fase2 enemy placed: {enemy.name} at {(Vector2)enemy.transform.position}, encounter={placement.Encounter}.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("Fase2 scene could not be saved.");
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Fase2 enemy encounters built successfully. Enemies={Placements.Length}, encounters={encounterRoots.Count}.");
    }

    private static Vector3 FindSafePosition(Tilemap ground, Vector2 desired)
    {
        var center = ground.WorldToCell(desired);
        for (var radius = 0; radius <= 10; radius++)
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
                    var blocked = Physics2D.OverlapCircleAll(world, 0.42f)
                        .Any(collider => collider.GetComponent<TilemapCollider2D>() != null || collider.GetComponent<EnemyAI2D>() != null);
                    if (!blocked)
                    {
                        return new Vector3(world.x, world.y, 0f);
                    }
                }
            }
        }

        throw new InvalidOperationException($"No safe floor position was found near {desired}.");
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
}
