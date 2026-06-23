using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Fase4EnemyEncounterBuilder
{
    private const string ScenePath = "Assets/Scenes/Fase4.unity";
    private const string CharacterRoot = "Assets/Characters/CraftPixRuinedTemple/Prepared";
    private const string EnemyRootName = "Enemies_Fase4";

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
        new("01_Entrada_Oeste", "Cultist1", new Vector2(-38f, -28f), 1.9f, 5.8f, 9f, 14),
        new("01_Entrada_Oeste", "Cultist2", new Vector2(-32f, -24f), 1.85f, 5.8f, 9f, 15),
        new("01_Entrada_Oeste", "Ghost", new Vector2(-26f, -29f), 1.7f, 6.3f, 10f, 18),

        new("02_Patrulha_Oeste", "Cultist3", new Vector2(-39f, -17f), 1.95f, 6f, 9f, 15),
        new("02_Patrulha_Oeste", "Cultist4", new Vector2(-30f, -13f), 1.9f, 6f, 9f, 16),
        new("02_Patrulha_Oeste", "Cultist1", new Vector2(-22f, -18f), 1.9f, 5.8f, 9f, 14),

        new("03_Galeria_Sul", "Cultist5", new Vector2(-15f, -27f), 1.95f, 6.1f, 10f, 16),
        new("03_Galeria_Sul", "Cultist6", new Vector2(-5f, -29f), 2f, 6.2f, 10f, 17),
        new("03_Galeria_Sul", "Cultist2", new Vector2(6f, -25f), 1.85f, 5.8f, 9f, 15),

        new("04_Patrulha_Leste", "Cultist3", new Vector2(17f, -27f), 1.95f, 6f, 9f, 15),
        new("04_Patrulha_Leste", "Cultist4", new Vector2(27f, -22f), 1.9f, 6f, 9f, 16),
        new("04_Patrulha_Leste", "Ghost", new Vector2(33f, -15f), 1.75f, 6.5f, 10f, 19),

        new("05_Circulos_Rituais", "Cultist5", new Vector2(-17f, -7f), 1.95f, 6.2f, 10f, 16),
        new("05_Circulos_Rituais", "Cultist6", new Vector2(-8f, -4f), 2f, 6.3f, 10f, 17),
        new("05_Circulos_Rituais", "Cultist1", new Vector2(4f, -7f), 1.9f, 5.9f, 9f, 15),
        new("05_Circulos_Rituais", "Cultist2", new Vector2(16f, -5f), 1.9f, 6f, 9f, 16),

        new("06_Guardas_Do_Altar", "Cultist3", new Vector2(-24f, -1f), 2f, 6.2f, 10f, 16),
        new("06_Guardas_Do_Altar", "Cultist4", new Vector2(-3f, -14f), 1.95f, 6.2f, 10f, 17),
        new("06_Guardas_Do_Altar", "Cultist5", new Vector2(14f, -14f), 2f, 6.3f, 10f, 17),
        new("06_Guardas_Do_Altar", "Cultist6", new Vector2(29f, -3f), 2.05f, 6.4f, 10f, 18),

        new("07_Confronto_Final", "Leader", new Vector2(2f, -1f), 1.75f, 7.2f, 12f, 24),
        new("07_Confronto_Final", "Ghost", new Vector2(2f, -10f), 1.8f, 6.8f, 11f, 20),
        new("07_Confronto_Final", "Cultist1", new Vector2(-7f, 1f), 2f, 6.2f, 10f, 16),
        new("07_Confronto_Final", "Cultist6", new Vector2(11f, 1f), 2.05f, 6.5f, 10f, 19),
        new("07_Confronto_Final", "Cultist4", new Vector2(23f, 0f), 2f, 6.3f, 10f, 18)
    };

    [MenuItem("Tools/Top Down/Build Fase 4 Enemy Encounters")]
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
            .FirstOrDefault(tilemap => tilemap.name == "Chao")
            ?? throw new InvalidOperationException("Fase4 Chao tilemap was not found.");
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
            enemy.GetComponent<EnemyAI2D>().Configure(placement.Speed, placement.Detection, placement.Leash, placement.Damage);
            Physics2D.SyncTransforms();
            Debug.Log($"Fase4 enemy placed: {enemy.name} at {(Vector2)enemy.transform.position}, encounter={placement.Encounter}.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("Fase4 scene could not be saved.");
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Fase4 enemy encounters built successfully. Enemies={Placements.Length}, encounters={encounterRoots.Count}.");
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
                    var blocked = Physics2D.OverlapBoxAll(world, new Vector2(0.6f, 0.8f), 0f)
                        .Any(collider => collider.GetComponent<TilemapCollider2D>() != null || collider.GetComponent<EnemyAI2D>() != null);
                    if (!blocked)
                    {
                        return new Vector3(world.x, world.y, 0f);
                    }
                }
            }
        }
        throw new InvalidOperationException($"No safe Fase4 floor position was found near {desired}.");
    }

    private static GameObject LoadCharacterPrefab(string character)
    {
        var path = $"{CharacterRoot}/{character}/Prefabs/{character}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ?? throw new InvalidOperationException($"CraftPix enemy prefab was not found: {path}");
    }
}
