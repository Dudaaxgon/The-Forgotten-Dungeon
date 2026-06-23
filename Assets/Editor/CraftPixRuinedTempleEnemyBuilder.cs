using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CraftPixRuinedTempleEnemyBuilder
{
    private const string Root = "Assets/Characters/CraftPixRuinedTemple";
    private const string SourceRoot = Root + "/Source";
    private const string PreparedRoot = Root + "/Prepared";
    private static readonly string[] Directions = { "Down", "Left", "Right", "Up" };

    [MenuItem("Tools/Characters/Build CraftPix Ruined Temple Enemies")]
    public static void BuildAll()
    {
        for (var i = 1; i <= 6; i++)
        {
            BuildCultist($"Cultist{i}");
        }

        BuildLeader();
        BuildGhost();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CraftPix Ruined Temple enemies prepared: 6 Cultists, Leader and Ghost.");
    }

    private static void BuildCultist(string name)
    {
        var characterRoot = PrepareCharacterFolders(name);
        var clips = new List<AnimationClip>();
        clips.AddRange(BuildDirectionalSheetClips(characterRoot, $"{SourceRoot}/{name}_Idle.png", "Idle", 12, 8f, true));
        clips.AddRange(BuildDirectionalSheetClips(characterRoot, $"{SourceRoot}/{name}_Walk.png", "Walk", 6, 10f, true));
        clips.AddRange(BuildDirectionalSheetClips(characterRoot, $"{SourceRoot}/{name}_Pray.png", "Attack", 12, 10f, false));
        BuildControllerAndPrefab(characterRoot, name, clips, false);
    }

    private static void BuildLeader()
    {
        const string name = "Leader";
        var characterRoot = PrepareCharacterFolders(name);
        var clips = new List<AnimationClip>();
        clips.AddRange(BuildDirectionalSheetClips(characterRoot, $"{SourceRoot}/{name}_Idle.png", "Idle", 12, 8f, true));
        clips.AddRange(BuildDirectionalSheetClips(characterRoot, $"{SourceRoot}/{name}_Walk.png", "Walk", 6, 10f, true));

        var summonPath = $"{SourceRoot}/{name}_summon.png";
        SliceSequentialSheet(summonPath, "Attack", 5, 3, 32, 32);
        var summonFrames = LoadSprites(summonPath, "Attack_");
        foreach (var direction in Directions)
        {
            clips.Add(BuildClip(characterRoot, $"Attack_{direction}", summonFrames, 10f, false));
        }

        BuildControllerAndPrefab(characterRoot, name, clips, false);
    }

    private static void BuildGhost()
    {
        const string name = "Ghost";
        var characterRoot = PrepareCharacterFolders(name);
        var sourcePath = $"{SourceRoot}/{name}.png";
        SliceSequentialSheet(sourcePath, "Ghost", 5, 4, 96, 128);
        var allFrames = LoadSprites(sourcePath, "Ghost_");
        var clips = new List<AnimationClip>();

        foreach (var direction in Directions)
        {
            clips.Add(BuildClip(characterRoot, $"Idle_{direction}", allFrames.Skip(5).Take(6).ToArray(), 7f, true));
            clips.Add(BuildClip(characterRoot, $"Walk_{direction}", allFrames.Skip(5).Take(10).ToArray(), 9f, true));
            clips.Add(BuildClip(characterRoot, $"Attack_{direction}", allFrames.Skip(10).Take(5).ToArray(), 9f, false));
            clips.Add(BuildClip(characterRoot, $"Death_{direction}", allFrames.Skip(15).Take(5).ToArray(), 8f, false));
        }

        BuildControllerAndPrefab(characterRoot, name, clips, true);
    }

    private static IEnumerable<AnimationClip> BuildDirectionalSheetClips(
        string characterRoot,
        string sourcePath,
        string state,
        int columns,
        float frameRate,
        bool loop)
    {
        SliceDirectionalSheet(sourcePath, state, columns);
        foreach (var direction in Directions)
        {
            var frames = LoadSprites(sourcePath, $"{state}_{direction}_");
            yield return BuildClip(characterRoot, $"{state}_{direction}", frames, frameRate, loop);
        }
    }

    private static void SliceDirectionalSheet(string assetPath, string state, int columns)
    {
        var definitions = new List<SpriteDefinition>();
        for (var row = 0; row < Directions.Length; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                definitions.Add(new SpriteDefinition(
                    $"{state}_{Directions[row]}_{column:00}",
                    new Rect(column * 32, (Directions.Length - row - 1) * 32, 32, 32)));
            }
        }
        ConfigureSpriteSheet(assetPath, definitions, 32f);
    }

    private static void SliceSequentialSheet(string assetPath, string prefix, int columns, int rows, int width, int height)
    {
        var definitions = new List<SpriteDefinition>();
        var index = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                definitions.Add(new SpriteDefinition(
                    $"{prefix}_{index++:00}",
                    new Rect(column * width, (rows - row - 1) * height, width, height)));
            }
        }
        ConfigureSpriteSheet(assetPath, definitions, 32f);
    }

    private static void ConfigureSpriteSheet(string assetPath, IReadOnlyCollection<SpriteDefinition> definitions, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter
            ?? throw new InvalidOperationException($"Texture importer was not found: {assetPath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existingIds = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID, StringComparer.Ordinal);
        var spriteRects = definitions.Select(definition => new SpriteRect
        {
            name = definition.Name,
            rect = definition.Rect,
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = existingIds.TryGetValue(definition.Name, out var id) ? id : GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(spriteRects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadSprites(string sourcePath, string prefix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(sourcePath)
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
    }

    private static AnimationClip BuildClip(string characterRoot, string clipName, IReadOnlyList<Sprite> sprites, float frameRate, bool loop)
    {
        if (sprites.Count == 0)
        {
            throw new InvalidOperationException($"No sprites found for {Path.GetFileName(characterRoot)}/{clipName}.");
        }

        var clipPath = $"{characterRoot}/Animations/{clipName}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName };
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        clip.frameRate = frameRate;

        var frames = sprites.Select((sprite, index) => new ObjectReferenceKeyframe
        {
            time = index / frameRate,
            value = sprite
        }).ToArray();
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BuildControllerAndPrefab(string characterRoot, string name, IEnumerable<AnimationClip> clips, bool largeCollider)
    {
        var clipArray = clips.OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
        var controllerPath = $"{characterRoot}/Animator/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
            ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = Array.Empty<ChildAnimatorState>();
        foreach (var clip in clipArray)
        {
            var state = stateMachine.AddState(clip.name);
            state.motion = clip;
            if (clip.name == "Idle_Down")
            {
                stateMachine.defaultState = state;
            }
        }

        var prefabPath = $"{characterRoot}/Prefabs/{name}.prefab";
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFirstSprite(clipArray.Single(clip => clip.name == "Idle_Down"));
        renderer.sortingOrder = 20;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = go.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = largeCollider ? new Vector2(0.9f, 1.35f) : new Vector2(0.55f, 0.8f);
        collider.offset = largeCollider ? new Vector2(0f, -0.45f) : new Vector2(0f, 0.05f);
        go.AddComponent<TopDownCharacterPhysics>();
        go.AddComponent<EnemyAI2D>();
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static Sprite GetFirstSprite(AnimationClip clip)
    {
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        return AnimationUtility.GetObjectReferenceCurve(clip, binding)[0].value as Sprite;
    }

    private static string PrepareCharacterFolders(string name)
    {
        var characterRoot = $"{PreparedRoot}/{name}";
        Directory.CreateDirectory($"{characterRoot}/Animations");
        Directory.CreateDirectory($"{characterRoot}/Animator");
        Directory.CreateDirectory($"{characterRoot}/Prefabs");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return characterRoot;
    }

    private readonly struct SpriteDefinition
    {
        public SpriteDefinition(string name, Rect rect)
        {
            Name = name;
            Rect = rect;
        }

        public string Name { get; }
        public Rect Rect { get; }
    }
}
