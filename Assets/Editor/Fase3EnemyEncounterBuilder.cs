using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Fase3EnemyEncounterBuilder
{
    private const string ScenePath = "Assets/Scenes/Fase3.unity";
    private const string CharacterRoot = "Assets/Characters/AutoImported";
    private const string EnemyRootName = "Enemies_Fase3";

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
        new("01_Entrada_Dos_Guardioes", "Skeleton3", new Vector2(-17f, -68f), 1.9f, 5.4f, 9f, 12),
        new("01_Entrada_Dos_Guardioes", "Zombie3", new Vector2(-8f, -64f), 1.5f, 5f, 8f, 14),
        new("01_Entrada_Dos_Guardioes", "Ghost1", new Vector2(1f, -68f), 1.8f, 5.8f, 9f, 14),

        new("02_Nave_Sul", "Orc3", new Vector2(10f, -58f), 1.75f, 5.8f, 9f, 16),
        new("02_Nave_Sul", "Lizardman3", new Vector2(22f, -55f), 1.8f, 6f, 10f, 16),
        new("02_Nave_Sul", "Imp1", new Vector2(34f, -62f), 2.05f, 6.2f, 10f, 15),

        new("03_Cripta_Oeste", "Skeleton3", new Vector2(-10f, -45f), 1.9f, 5.5f, 9f, 12),
        new("03_Cripta_Oeste", "Golem2", new Vector2(-1f, -42f), 1.25f, 5.4f, 9f, 18),
        new("03_Cripta_Oeste", "Ghost2", new Vector2(7f, -47f), 1.85f, 6f, 10f, 15),

        new("04_Cripta_Leste", "Orc3", new Vector2(43f, -47f), 1.75f, 5.8f, 9f, 16),
        new("04_Cripta_Leste", "Golem3", new Vector2(53f, -43f), 1.3f, 5.6f, 9f, 19),
        new("04_Cripta_Leste", "Vampires1", new Vector2(48f, -38f), 1.95f, 6.2f, 10f, 17),

        new("05_Salao_Central", "Lizardman2", new Vector2(17f, -33f), 1.8f, 6f, 10f, 16),
        new("05_Salao_Central", "Skeleton2", new Vector2(29f, -30f), 1.85f, 5.5f, 9f, 12),
        new("05_Salao_Central", "Imp2", new Vector2(39f, -34f), 2.1f, 6.3f, 10f, 16),
        new("05_Salao_Central", "Beholder1", new Vector2(25f, -39f), 1.55f, 6.5f, 10f, 18),

        new("06_Flancos_Dos_Guardioes", "Golem3", new Vector2(0f, -20f), 1.3f, 5.8f, 9f, 19),
        new("06_Flancos_Dos_Guardioes", "Golem2", new Vector2(50f, -20f), 1.25f, 5.8f, 9f, 18),
        new("06_Flancos_Dos_Guardioes", "Ghost3", new Vector2(8f, -16f), 1.9f, 6.2f, 10f, 16),
        new("06_Flancos_Dos_Guardioes", "Vampires2", new Vector2(43f, -16f), 2f, 6.3f, 10f, 18),

        new("07_Ala_Dos_Magos", "Skeleton3", new Vector2(7f, -6f), 1.9f, 5.6f, 9f, 13),
        new("07_Ala_Dos_Magos", "Orc3", new Vector2(42f, -6f), 1.75f, 5.9f, 9f, 17),
        new("07_Ala_Dos_Magos", "Lich1", new Vector2(15f, -10f), 1.6f, 6.6f, 11f, 20),
        new("07_Ala_Dos_Magos", "Lich2", new Vector2(34f, -10f), 1.65f, 6.7f, 11f, 21),

        new("08_Trono_Dos_Guardioes", "Beholder2", new Vector2(16f, 3f), 1.6f, 6.8f, 11f, 20),
        new("08_Trono_Dos_Guardioes", "Vampires3", new Vector2(31f, 3f), 2.05f, 6.6f, 11f, 19),
        new("08_Trono_Dos_Guardioes", "Lich3", new Vector2(24f, 0f), 1.7f, 7f, 12f, 23)
    };

    [MenuItem("Tools/Top Down/Build Fase 3 Enemy Encounters")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TopDownSceneCollisionBootstrap.ConfigureSceneColliders(scene);
        foreach (var collider in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>(true)))
        {
            collider.ProcessTilemapChanges();
        }
        Physics2D.SyncTransforms();

        var ground = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => tilemap.name == "Chao");
        if (ground == null)
        {
            throw new InvalidOperationException("Fase3 Chao tilemap was not found.");
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
            Debug.Log($"Fase3 enemy placed: {enemy.name} at {(Vector2)enemy.transform.position}, encounter={placement.Encounter}.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("Fase3 scene could not be saved.");
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Fase3 enemy encounters built successfully. Enemies={Placements.Length}, encounters={encounterRoots.Count}.");
    }

    private static Vector3 FindSafePosition(Tilemap ground, Vector2 desired)
    {
        var center = ground.WorldToCell(desired);
        for (var radius = 0; radius <= 12; radius++)
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
