using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TopDownPhysicsConfigurator
{
    private static readonly string[] CharacterPrefabRoots =
    {
        "Assets/Characters/AutoImported"
    };

    private static readonly string[] SolidTilemapNameParts =
    {
        "parede",
        "porta",
        "janela",
        "acabamento",
        "buraco",
        "armadilha",
        "espinho",
        "spike"
    };

    [MenuItem("Tools/Top Down/Configure Character Physics")]
    public static void ConfigureCharacterPrefabs()
    {
        var prefabGuids = CharacterPrefabRoots
            .Where(AssetDatabase.IsValidFolder)
            .SelectMany(root => AssetDatabase.FindAssets("t:Prefab", new[] { root }))
            .Distinct()
            .ToArray();

        var configured = 0;
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                ConfigureCharacter(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                configured++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"TopDownPhysicsConfigurator configured {configured} character prefabs.");
    }

    [MenuItem("Tools/Top Down/Configure Open Scene Collision")]
    public static void ConfigureOpenSceneCollision()
    {
        var scene = SceneManager.GetActiveScene();
        var configured = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (!ShouldBeSolid(tilemap.gameObject.name))
                {
                    continue;
                }

                ConfigureSolidTilemap(tilemap.gameObject);
                configured++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"TopDownPhysicsConfigurator configured {configured} solid tilemaps in {scene.name}.");
    }

    [MenuItem("Tools/Top Down/Configure All")]
    public static void ConfigureAll()
    {
        ConfigureCharacterPrefabs();
        ConfigureOpenSceneCollision();
    }

    private static void ConfigureCharacter(GameObject root)
    {
        var body = root.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = root.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = root.GetComponent<Collider2D>();
        if (collider == null)
        {
            var capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.55f, 0.8f);
            capsule.offset = new Vector2(0f, 0.05f);
            collider = capsule;
        }

        collider.isTrigger = false;
        collider.usedByComposite = false;

        if (root.GetComponent<TopDownCharacterPhysics>() == null)
        {
            root.AddComponent<TopDownCharacterPhysics>();
        }
    }

    private static bool ShouldBeSolid(string objectName)
    {
        var normalized = objectName.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        return SolidTilemapNameParts.Any(normalized.Contains);
    }

    private static void ConfigureSolidTilemap(GameObject tilemapObject)
    {
        var tilemapCollider = tilemapObject.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
        }

        tilemapCollider.isTrigger = false;
        tilemapCollider.usedByComposite = true;

        var body = tilemapObject.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = tilemapObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        var composite = tilemapObject.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = tilemapObject.AddComponent<CompositeCollider2D>();
        }

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = false;
    }
}
